using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Builds;
using Bot.Debugging.GraphicalDebugging;
using Bot.GameData;
using Bot.GameSense;
using Bot.Managers.EconomyManagement.TownHallSupervision;
using Bot.Utils;

namespace Bot.Managers.EconomyManagement;

public sealed partial class EconomyManager : Manager {
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IBuildingTracker _buildingTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly ICreepTracker _creepTracker;
    private readonly IEconomySupervisorFactory _economySupervisorFactory;
    private readonly IGraphicalDebugger _graphicalDebugger;

    private const int MaxDroneCount = 70;
    private readonly BuildManager _buildManager;

    private const uint GasDroneCountLoweringDelay = (int)(TimeUtils.FramesPerSecond * 15);
    private int _requiredDronesInGas = 0;
    private uint _doNotChangeGasDroneCountBefore = 0;

    private readonly HashSet<TownHallSupervisor> _townHallSupervisors = new HashSet<TownHallSupervisor>();

    private readonly HashSet<Unit> _townHalls = new HashSet<Unit>();
    private readonly HashSet<Unit> _workers = new HashSet<Unit>();

    private int _creepQueensCount = 1;

    private readonly BuildRequest _expandBuildRequest;
    private readonly BuildRequest _macroHatchBuildRequest;
    private readonly BuildRequest _queenBuildRequest;
    private readonly BuildRequest _extractorsBuildRequest;
    private readonly BuildRequest _dronesBuildRequest;
    private readonly List<BuildRequest> _buildRequests = new List<BuildRequest>();

    public override IEnumerable<BuildFulfillment> BuildFulfillments => _buildRequests.Select(buildRequest => buildRequest.Fulfillment)
        .Concat(_townHallSupervisors.SelectMany(supervisor => supervisor.BuildFulfillments));

    protected override IAssigner Assigner { get; }
    protected override IDispatcher Dispatcher { get; }
    protected override IReleaser Releaser { get; }

    public EconomyManager(
        BuildManager buildManager,
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IBuildingTracker buildingTracker,
        IRegionsTracker regionsTracker,
        ICreepTracker creepTracker,
        IEconomySupervisorFactory economySupervisorFactory,
        IBuildRequestFactory buildRequestFactory,
        IGraphicalDebugger graphicalDebugger
    ) {
        _buildManager = buildManager;
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _buildingTracker = buildingTracker;
        _regionsTracker = regionsTracker;
        _creepTracker = creepTracker;
        _economySupervisorFactory = economySupervisorFactory;
        _graphicalDebugger = graphicalDebugger;

        Assigner = new EconomyManagerAssigner(this);
        Dispatcher = new EconomyManagerDispatcher(this);
        Releaser = new EconomyManagerReleaser(this);

        _expandBuildRequest = buildRequestFactory.CreateQuantityBuildRequest(BuildType.Expand, Units.Hatchery, quantity: 0, blockCondition: BuildBlockCondition.MissingResources, priority: BuildRequestPriority.High);
        _buildRequests.Add(_expandBuildRequest);

        // TODO GD Need to differentiate macro and mining townhalls
        _macroHatchBuildRequest = buildRequestFactory.CreateTargetBuildRequest(BuildType.Build, Units.Hatchery, targetQuantity: _townHalls.Count);
        _buildRequests.Add(_macroHatchBuildRequest);

        _queenBuildRequest = buildRequestFactory.CreateTargetBuildRequest(BuildType.Train, Units.Queen, targetQuantity: 0);
        _buildRequests.Add(_queenBuildRequest);

        _extractorsBuildRequest = buildRequestFactory.CreateTargetBuildRequest(BuildType.Build, Units.Extractor, targetQuantity: 0);
        _buildRequests.Add(_extractorsBuildRequest);

        _dronesBuildRequest = buildRequestFactory.CreateTargetBuildRequest(BuildType.Train, Units.Drone, targetQuantity: 0);
        _buildRequests.Add(_dronesBuildRequest);
    }

    protected override void RecruitmentPhase() {
        var unmanagedTownHalls = Controller.GetUnits(_unitsTracker.OwnedUnits, Units.TownHalls).Where(unit => unit.Manager == null);
        Assign(unmanagedTownHalls);

        var unmanagedQueens = Controller.GetUnits(_unitsTracker.OwnedUnits, Units.Queen).Where(unit => unit.Manager == null);
        Assign(unmanagedQueens);

        var unmanagedIdleWorkers = Controller.GetUnits(_unitsTracker.OwnedUnits, Units.Workers)
            .Where(unit => unit.Manager == null)
            // We do this to not select drones that are going to build something
            // TODO GD This is problematic because we need to send a stop order whenever we release a drone
            .Where(unit => !unit.OrdersExceptMining.Any());

        Assign(unmanagedIdleWorkers);
    }

    protected override void DispatchPhase() {
        Dispatch(ManagedUnits.Where(unit => unit.Supervisor == null));
        EqualizeWorkers();
    }

    protected override void ManagementPhase() {
        AdjustGasProduction();
        foreach (var townHallSupervisor in _townHallSupervisors) {
            townHallSupervisor.OnFrame();
        }

        if (ShouldExpand()) {
            _expandBuildRequest.Requested += 1;
        }

        if (ShouldBuildExtraMacroHatch()) {
            _macroHatchBuildRequest.Requested += 1;
        }

        AdjustCreepQueensCount();

        _queenBuildRequest.Requested = Controller.GetUnits(_unitsTracker.OwnedUnits, Units.Hatchery).Count() + _creepQueensCount;
        _dronesBuildRequest.Requested = Math.Min(MaxDroneCount, _townHallSupervisors.Sum(supervisor => !supervisor.TownHall.IsOperational ? 0 : supervisor.SaturatedCapacity));
    }

    private void AdjustCreepQueensCount() {
        if (Controller.CurrentSupply >= 130) {
            _creepQueensCount = 3;
        }
        else if (Controller.CurrentSupply >= 100) {
            _creepQueensCount = 2;
        }
    }

    /// <summary>
    /// Adjust the gas production based on the expected future spend.
    /// The goal is to have enough gas so that we're not flooding it, but also not bottle-necked by it.
    /// </summary>
    private void AdjustGasProduction() {
        // The build order is optimized, take all the gas we can
        if (!_buildManager.IsBuildOrderDone) {
            // There can be scenarios where we lose most of our drones early game
            // When that happens, we must really focus on minerals
            var hasEnoughDronesForGas = ManagedUnits.Count(unit => unit.UnitType == Units.Drone) >= 10;

            foreach (var townHallSupervisor in _townHallSupervisors) {
                townHallSupervisor.GasWorkersCap = hasEnoughDronesForGas ? townHallSupervisor.MaxGasCapacity : 0;
            }

            return;
        }

        var requiredDronesInGas = ComputeRequiredGasDroneCount();
        if (requiredDronesInGas == _requiredDronesInGas || Controller.Frame < _doNotChangeGasDroneCountBefore) {
            return;
        }

        var extractorsNeeded = (int)Math.Ceiling(requiredDronesInGas / (double)Resources.MaxDronesPerExtractor);
        _extractorsBuildRequest.Requested = extractorsNeeded;

        _requiredDronesInGas = requiredDronesInGas;
        _doNotChangeGasDroneCountBefore = Controller.Frame + GasDroneCountLoweringDelay;

        foreach (var townHallSupervisor in _townHallSupervisors) {
            var newGasWorkersCap = Math.Min(requiredDronesInGas, townHallSupervisor.MaxGasCapacity);

            townHallSupervisor.GasWorkersCap = newGasWorkersCap;
            requiredDronesInGas -= newGasWorkersCap;
        }
    }

    /// <summary>
    /// Compute the ideal number of drones that should mine gas based on our future spend and current income.
    /// </summary>
    /// <returns>The number of drones that should be sent to gas mining.</returns>
    private static int ComputeRequiredGasDroneCount() {
        var totalSpend = SpendingTracker.Instance.ExpectedFutureMineralsSpending + SpendingTracker.Instance.ExpectedFutureVespeneSpending;
        if (totalSpend == 0) {
            return 0;
        }

        var gasSpendRatio = SpendingTracker.Instance.ExpectedFutureVespeneSpending / totalSpend;

        var totalIncome = SpendingTracker.Instance.ExpectedFutureMineralsSpending + SpendingTracker.Instance.ExpectedFutureVespeneSpending;

        // We subtract the available gas to offset any income/spend evaluation error.
        // If our resource management is perfect, we'll have near 0 gas all the time, so subtracting will have no impact at all.
        // Otherwise, if we start flooding gas, we'll just diminish our target gas income.
        // The current implementation won't work if we need more gas than minerals, we'll fix it if it ever happens.
        var gasFlood = Math.Max(0, Controller.AvailableVespene - Controller.AvailableMinerals);
        var targetGasIncome = Math.Max(0, totalIncome * gasSpendRatio - gasFlood);
        var oneDroneInGasIncome = IncomeTracker.ComputeResourceNodeExpectedCollectionRate(Resources.ResourceType.Gas, 1);

        return (int)Math.Ceiling(targetGasIncome / oneDroneInGasIncome);
    }

    // TODO GD This doesn't seem to work very well
    private void EqualizeWorkers() {
        var supervisorInNeed = GetClosestSupervisorWithIdealCapacityNotMet(_terrainTracker.StartingLocation);
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

            supervisorInNeed = GetClosestSupervisorWithIdealCapacityNotMet(_terrainTracker.StartingLocation);
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

    private IEnumerable<Unit> GetIdleLarvae() {
        return Controller.GetUnits(_unitsTracker.OwnedUnits, Units.Larva)
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

    private IEnumerable<Unit> GetTownHallsInConstruction() {
        return Controller.GetUnits(_unitsTracker.OwnedUnits, Units.Hatchery)
            .Where(townHall => !townHall.IsOperational)
            .Concat(Controller.GetProducersCarryingOrders(Units.Hatchery));
    }

    public override string ToString() {
        return "EconomyManager";
    }
}
