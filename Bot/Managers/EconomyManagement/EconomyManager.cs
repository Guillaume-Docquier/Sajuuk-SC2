using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Builds;
using Bot.GameData;
using Bot.GameSense;
using Bot.Managers.EconomyManagement.TownHallSupervision;
using Bot.MapKnowledge;

namespace Bot.Managers.EconomyManagement;

public sealed partial class EconomyManager: Manager {
    private const int MaxDroneCount = 70;

    private readonly HashSet<TownHallSupervisor> _townHallSupervisors = new HashSet<TownHallSupervisor>();

    private readonly HashSet<Unit> _townHalls = new HashSet<Unit>();
    private readonly HashSet<Unit> _workers = new HashSet<Unit>();

    private int _creepQueensCount = 1;

    private readonly BuildRequest _expandBuildRequest = new QuantityBuildRequest(BuildType.Expand, Units.Hatchery, quantity: 0, isBlocking: true, priority: BuildRequestPriority.Important);
    private readonly BuildRequest _macroHatchBuildRequest = new TargetBuildRequest(BuildType.Build, Units.Hatchery, targetQuantity: 0);
    private readonly BuildRequest _queenBuildRequest = new TargetBuildRequest(BuildType.Train, Units.Queen, targetQuantity: 0);
    private readonly BuildRequest _dronesBuildRequest = new TargetBuildRequest(BuildType.Train, Units.Drone, targetQuantity: 0);
    private readonly List<BuildRequest> _buildRequests = new List<BuildRequest>();

    public override IEnumerable<BuildFulfillment> BuildFulfillments => _buildRequests.Select(buildRequest => buildRequest.Fulfillment)
        .Concat(_townHallSupervisors.SelectMany(supervisor => supervisor.BuildFulfillments));

    protected override IAssigner Assigner { get; }
    protected override IDispatcher Dispatcher { get; }
    protected override IReleaser Releaser { get; }

    public EconomyManager() {
        Assigner = new EconomyManagerAssigner(this);
        Dispatcher = new EconomyManagerDispatcher(this);
        Releaser = new EconomyManagerReleaser(this);

        _macroHatchBuildRequest.Requested = _townHalls.Count; // TODO GD Need to differentiate macro and mining bases

        _buildRequests.Add(_expandBuildRequest);
        _buildRequests.Add(_macroHatchBuildRequest);
        _buildRequests.Add(_queenBuildRequest);
        _buildRequests.Add(_dronesBuildRequest);
    }

    protected override void AssignmentPhase() {
        var unmanagedTownHalls = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.TownHalls).Where(unit => unit.Manager == null);
        Assign(unmanagedTownHalls);

        var unmanagedQueens = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Queen).Where(unit => unit.Manager == null);
        Assign(unmanagedQueens);

        var unmanagedIdleWorkers = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Workers)
            .Where(unit => unit.Manager == null)
            .Where(unit => !unit.OrdersExceptMining.Any());

        Assign(unmanagedIdleWorkers);
    }

    protected override void DispatchPhase() {
        Dispatch(ManagedUnits.Where(unit => unit.Supervisor == null));
        EqualizeWorkers();
    }

    protected override void ManagementPhase() {
        foreach (var townHallSupervisor in _townHallSupervisors) {
            townHallSupervisor.OnFrame();
        }

        if (ShouldExpand()) {
            _expandBuildRequest.Requested += 1;
        }

        if (ShouldBuildExtraMacroHatch()) {
            _macroHatchBuildRequest.Requested += 1;
        }

        if (Controller.CurrentSupply >= 130) {
            _creepQueensCount = 3;
        }
        else if (Controller.CurrentSupply >= 100) {
            _creepQueensCount = 2;
        }

        _queenBuildRequest.Requested = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery).Count() + _creepQueensCount;
        _dronesBuildRequest.Requested = Math.Min(MaxDroneCount, _townHallSupervisors.Sum(supervisor => !supervisor.TownHall.IsOperational ? 0 : supervisor.SaturatedCapacity));
    }

    // TODO GD This doesn't seem to work very well
    private void EqualizeWorkers() {
        var supervisorInNeed = GetClosestSupervisorWithIdealCapacityNotMet(MapAnalyzer.StartingLocation);
        while (supervisorInNeed != null) {
            var requiredWorkers = supervisorInNeed.IdealAvailableCapacity;
            var supervisorWithExtraWorkers = _townHallSupervisors.FirstOrDefault(supervisor => supervisor.IdealAvailableCapacity < 0); // Negative IdealAvailableCapacity means they have extra workers
            if (supervisorWithExtraWorkers == null) {
                break;
            }

            var nbWorkersToRelease = Math.Min(-1 * supervisorWithExtraWorkers.IdealAvailableCapacity, requiredWorkers);
            foreach (var freeWorker in supervisorWithExtraWorkers.HandOutWorkers(nbWorkersToRelease)) {
                supervisorInNeed.Assign(freeWorker);
            }

            supervisorInNeed = GetClosestSupervisorWithIdealCapacityNotMet(MapAnalyzer.StartingLocation);
        }
    }

    private TownHallSupervisor GetClosestSupervisorWithIdealCapacityNotMet(Vector2 position) {
        return GetAvailableSupervisors()
            .Where(manager => manager.IdealAvailableCapacity > 0)
            .MinBy(manager => manager.TownHall.DistanceTo(position));
    }

    private TownHallSupervisor GetClosestSupervisorWithSaturatedCapacityNotMet(Vector2 position) {
        return GetAvailableSupervisors()
            .Where(manager => manager.SaturatedAvailableCapacity > 0)
            .MinBy(manager => manager.TownHall.DistanceTo(position));
    }

    private TownHallSupervisor GetClosestSupervisorWithNoQueen(Unit queen) {
        return GetAvailableSupervisors()
            .OrderBy(manager => manager.TownHall.DistanceTo(queen))
            .FirstOrDefault(manager => manager.Queen == null);
    }

    private IEnumerable<TownHallSupervisor> GetAvailableSupervisors() {
        return _townHallSupervisors.Where(manager => manager.TownHall.IsOperational);
    }

    /// <summary>
    /// Determines if an expand should be requested.
    /// Will return true if there are idle workers and no other expansion has been requested or is in progress
    /// </summary>
    /// <returns>True if an expand should be requested</returns>
    private bool ShouldExpand() {
        if (_workers.All(worker => worker.Supervisor != null)) {
            return false;
        }

        // TODO GD We could request 2 if we have a lot of idle workers
        if (_expandBuildRequest.Fulfillment.Remaining > 0) {
            return false;
        }

        // TODO GD Don't consider only macro hatches
        if (GetTownHallsInConstruction().Any()) {
            return false;
        }

        return true;
    }

    ////////////////////////////////
    //                            //
    //   Macro Hatch Evaluation   //
    //                            //
    ////////////////////////////////

    private bool ShouldBuildExtraMacroHatch() {
        return _macroHatchBuildRequest.Fulfillment.Remaining == 0
               && BankIsTooBig()
               && !GetIdleLarvae().Any()
               && !HasReachedMaximumMacroTownHalls()
               && !GetTownHallsInConstruction().Any();
    }

    private static IEnumerable<Unit> GetIdleLarvae() {
        return Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Larva)
            .Where(larva => !larva.Orders.Any());
    }

    // TODO GD Should this increase for every macro hatch built?
    private static bool BankIsTooBig() {
        return Controller.AvailableMinerals > KnowledgeBase.GetUnitTypeData(Units.Hatchery).MineralCost * 2;
    }

    private bool HasReachedMaximumMacroTownHalls() {
        var nbTownHalls = _townHalls.Count
                          + Controller.GetProducersCarryingOrders(Units.Hatchery).Count();

        return nbTownHalls >= Controller.GetMiningTownHalls().Count() * 2;
    }

    private static IEnumerable<Unit> GetTownHallsInConstruction() {
        return Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery)
            .Where(townHall => !townHall.IsOperational)
            .Concat(Controller.GetProducersCarryingOrders(Units.Hatchery));
    }

    public override string ToString() {
        return "EconomyManager";
    }
}
