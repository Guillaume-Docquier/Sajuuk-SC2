using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Builds;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapKnowledge;

namespace Bot.Managers;

public sealed partial class EconomyManager: Manager {
    private const int MaxDroneCount = 70;

    private readonly HashSet<TownHallSupervisor> _townHallSupervisors = new HashSet<TownHallSupervisor>();

    private readonly HashSet<Unit> _townHalls = new HashSet<Unit>();
    private readonly HashSet<Unit> _queens = new HashSet<Unit>();
    private readonly HashSet<Unit> _workers = new HashSet<Unit>();

    private readonly BuildRequest _macroHatchBuildRequest = new TargetBuildRequest(BuildType.Build, Units.Hatchery, targetQuantity: 0);
    private readonly BuildRequest _queenBuildRequest = new TargetBuildRequest(BuildType.Train, Units.Queen, targetQuantity: 0);
    private readonly BuildRequest _dronesBuildRequest = new TargetBuildRequest(BuildType.Train, Units.Drone, targetQuantity: 0);
    private readonly List<BuildRequest> _buildStepRequests = new List<BuildRequest>();

    private int _creepQueensCount = 1;

    public override IEnumerable<BuildFulfillment> BuildFulfillments => _buildStepRequests.Select(buildRequest => buildRequest.Fulfillment)
        .Concat(_townHallSupervisors.SelectMany(supervisor => supervisor.BuildFulfillments));


    public static EconomyManager Create() {
        var manager = new EconomyManager();
        manager.Init();

        return manager;
    }

    private EconomyManager() {
        _macroHatchBuildRequest.Requested = _townHalls.Count; // TODO GD Need to differentiate macro and mining bases

        _buildStepRequests.Add(_macroHatchBuildRequest);
        _buildStepRequests.Add(_queenBuildRequest);
        _buildStepRequests.Add(_dronesBuildRequest);
    }

    protected override IAssigner CreateAssigner() {
        return new EconomyManagerAssigner(this);
    }

    protected override IDispatcher CreateDispatcher() {
        return new EconomyManagerDispatcher(this);
    }

    protected override IReleaser CreateReleaser() {
        return new EconomyManagerReleaser(this);
    }

    protected override void AssignUnits() {
        var unmanagedTownHalls = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.TownHalls).Where(unit => unit.Manager == null);
        Assign(unmanagedTownHalls);

        var unmanagedQueens = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Queen).Where(unit => unit.Manager == null);
        Assign(unmanagedQueens);

        var unmanagedIdleWorkers = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Workers)
            .Where(unit => unit.Manager == null)
            .Where(unit => !unit.OrdersExceptMining.Any());

        Assign(unmanagedIdleWorkers);
    }

    protected override void DispatchUnits() {
        Dispatch(_townHalls.Where(townHall => townHall.Supervisor == null));
        Dispatch(_queens.Where(queen => queen.Supervisor == null));
        Dispatch(_workers.Where(worker => worker.Supervisor == null));

        EqualizeWorkers();
    }

    protected override void Manage() {
        foreach (var townHallSupervisor in _townHallSupervisors) {
            townHallSupervisor.OnFrame();
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

    private TownHallSupervisor GetClosestSupervisorWithIdealCapacityNotMet(Vector3 position) {
        return GetAvailableSupervisors()
            .Where(manager => manager.IdealAvailableCapacity > 0)
            .MinBy(manager => manager.TownHall.DistanceTo(position));
    }

    private TownHallSupervisor GetClosestSupervisorWithSaturatedCapacityNotMet(Vector3 position) {
        return GetAvailableSupervisors()
            .Where(manager => manager.SaturatedAvailableCapacity > 0)
            .MinBy(manager => manager.TownHall.DistanceTo(position));
    }

    private TownHallSupervisor GetSupervisorWithHighestAvailableCapacity() {
        return GetAvailableSupervisors().MaxBy(manager => manager.SaturatedAvailableCapacity);
    }

    private TownHallSupervisor GetClosestSupervisorWithNoQueen(Unit queen) {
        return GetAvailableSupervisors()
            .OrderBy(manager => manager.TownHall.DistanceTo(queen))
            .FirstOrDefault(manager => manager.Queen == null);
    }

    private IEnumerable<TownHallSupervisor> GetAvailableSupervisors() {
        return _townHallSupervisors.Where(manager => manager.TownHall.IsOperational);
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
