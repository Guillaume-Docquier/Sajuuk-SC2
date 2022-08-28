using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Builds;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapKnowledge;
using Bot.UnitModules;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.Managers;

public class EconomyManager: IManager {
    private const int MaxDroneCount = 70;

    private readonly List<TownHallManager> _townHallManagers = new List<TownHallManager>();
    private readonly Dictionary<Unit, TownHallManager> _townHallDispatch = new Dictionary<Unit, TownHallManager>();
    private readonly Dictionary<Unit, TownHallManager> _queenDispatch = new Dictionary<Unit, TownHallManager>();
    private readonly Dictionary<Unit, TownHallManager> _workerDispatch = new Dictionary<Unit, TownHallManager>();
    private readonly List<Color> _expandColors = new List<Color>
    {
        Colors.MaroonRed,
        Colors.BurlywoodBeige,
        Colors.CornflowerBlue,
        Colors.DarkGreen,
        Colors.DarkBlue,
    };

    private readonly BuildRequest _macroHatchBuildRequest = new TargetBuildRequest(BuildType.Build, Units.Hatchery, targetQuantity: 0);
    private readonly BuildRequest _queenBuildRequest = new TargetBuildRequest(BuildType.Train, Units.Queen, targetQuantity: 0);
    private readonly BuildRequest _dronesBuildRequest = new TargetBuildRequest(BuildType.Train, Units.Drone, targetQuantity: 0);
    private readonly List<BuildRequest> _buildStepRequests = new List<BuildRequest>();

    private int _creepQueensCount = 1;

    public IEnumerable<BuildFulfillment> BuildFulfillments => _buildStepRequests.Select(buildRequest => buildRequest.Fulfillment)
        .Concat(_townHallManagers.SelectMany(manager => manager.BuildFulfillments));

    public IEnumerable<Unit> ManagedUnits => _workerDispatch.Keys
        .Concat(_queenDispatch.Keys)
        .Concat(_townHallDispatch.Keys);

    public EconomyManager() {
        ManageTownHalls(Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery));
        DispatchWorkers(Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Drone).ToList());

        _macroHatchBuildRequest.Requested = _townHallDispatch.Count;

        _buildStepRequests.Add(_macroHatchBuildRequest);
        _buildStepRequests.Add(_queenBuildRequest);
        _buildStepRequests.Add(_dronesBuildRequest);
    }

    public void OnFrame() {
        ManageTownHalls(Controller.GetUnits(UnitsTracker.NewOwnedUnits, Units.Hatchery));

        if (_townHallManagers.Count > 0) {
            // TODO GD Some idle workers are not picked up, i.e when their building order failed. They were released and they won't be new units again
            var workersToDispatch = Controller.GetUnits(UnitsTracker.NewOwnedUnits, Units.Drone).ToList();
            workersToDispatch.AddRange(GetIdleWorkers());
            DispatchWorkers(workersToDispatch);

            EqualizeWorkers();

            var queensToDispatch = Controller.GetUnits(UnitsTracker.NewOwnedUnits, Units.Queen).ToList();
            queensToDispatch.AddRange(GetIdleQueens());
            DispatchQueens(queensToDispatch);

            // Execute managers
            _townHallManagers.ForEach(manager => manager.OnFrame());
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
        _dronesBuildRequest.Requested = Math.Min(MaxDroneCount, _townHallManagers.Sum(manager => !manager.TownHall.IsOperational ? 0 : manager.SaturatedCapacity));
    }

    public void Release(Unit unit) {
        if (_workerDispatch.TryGetValue(unit, out var townHallManager)) {
            _workerDispatch.Remove(unit);
        }
        else if (_queenDispatch.TryGetValue(unit, out townHallManager)) {
            _queenDispatch.Remove(unit);
        }

        if (townHallManager != null) {
            unit.Manager = null; // TODO GD This might will not work because Release() can happen when another manager takes over?
            townHallManager.Release(unit);
            unit.RemoveDeathWatcher(this);
        }
    }

    public void Retire() {
        throw new NotImplementedException();
    }

    public void ReportUnitDeath(Unit deadUnit) {
        switch (deadUnit.UnitType) {
            case Units.Hatchery:
            case Units.Lair:
            case Units.Hive:
                var manager = _townHallDispatch[deadUnit];
                _workerDispatch
                    .Where(dispatch => dispatch.Value == manager)
                    .ToList()
                    .ForEach(dispatch => _workerDispatch[dispatch.Key] = null);

                _townHallDispatch.Remove(deadUnit);
                _townHallManagers.Remove(manager);
                manager.Retire();
                break;
            case Units.Drone:
                _workerDispatch.Remove(deadUnit);
                break;
            case Units.Queen:
                _queenDispatch.Remove(deadUnit);
                break;
        }
    }

    private void ManageTownHalls(IEnumerable<Unit> townHalls) {
        foreach (var townHall in townHalls) {
            townHall.AddDeathWatcher(this);
            townHall.Manager = this;

            var miningManager = new TownHallManager(townHall, GetNewExpandColor());
            _townHallDispatch[townHall] = miningManager;
            _townHallManagers.Add(miningManager);
        }
    }

    private void DispatchWorkers(List<Unit> workers) {
        foreach (var worker in workers) {
            worker.AddDeathWatcher(this);
            worker.Manager = this;

            var manager = GetClosestManagerWithIdealCapacityNotMet(worker.Position);
            manager ??= GetClosestManagerWithSaturatedCapacityNotMet(worker.Position);
            manager ??= GetManagerWithHighestAvailableCapacity();

            _workerDispatch[worker] = manager;
            manager?.AssignWorker(worker);
        }
    }

    private void EqualizeWorkers() {
        var managerInNeed = GetClosestManagerWithIdealCapacityNotMet(MapAnalyzer.StartingLocation);
        while (managerInNeed != null) {
            var requiredWorkers = managerInNeed.IdealAvailableCapacity;
            var managerWithExtraWorkers = _townHallManagers.FirstOrDefault(manager => manager.IdealAvailableCapacity < 0); // Negative IdealAvailableCapacity means they have extra workers
            if (managerWithExtraWorkers == null) {
                break;
            }

            var nbWorkersToRelease = Math.Min(-1 * managerWithExtraWorkers.IdealAvailableCapacity, requiredWorkers);
            foreach (var freeWorker in managerWithExtraWorkers.HandOutWorkers(nbWorkersToRelease)) {
                managerInNeed.AssignWorker(freeWorker);
            }

            managerInNeed = GetClosestManagerWithIdealCapacityNotMet(MapAnalyzer.StartingLocation);
        }
    }

    private void DispatchQueens(List<Unit> queens) {
        foreach (var queen in queens) {
            queen.AddDeathWatcher(this);
            queen.Manager = this;

            if (!_queenDispatch.ContainsKey(queen)) {
                QueenMicroModule.Install(queen);
                ChangelingTargetingModule.Install(queen);
            }

            var manager = GetClosestManagerWithNoQueen(queen);
            manager?.AssignQueen(queen);
            _queenDispatch[queen] = manager;
        }
    }

    private TownHallManager GetClosestManagerWithIdealCapacityNotMet(Vector3 position) {
        return GetAvailableManagers()
            .Where(manager => manager.IdealAvailableCapacity > 0)
            .MinBy(manager => manager.TownHall.DistanceTo(position));
    }

    private TownHallManager GetClosestManagerWithSaturatedCapacityNotMet(Vector3 position) {
        return GetAvailableManagers()
            .Where(manager => manager.SaturatedAvailableCapacity > 0)
            .MinBy(manager => manager.TownHall.DistanceTo(position));
    }

    private TownHallManager GetManagerWithHighestAvailableCapacity() {
        return GetAvailableManagers().MaxBy(manager => manager.SaturatedAvailableCapacity);
    }

    private TownHallManager GetClosestManagerWithNoQueen(Unit queen) {
        return GetAvailableManagers()
            .OrderBy(manager => manager.TownHall.DistanceTo(queen))
            .FirstOrDefault(manager => manager.Queen == null);
    }

    private IEnumerable<TownHallManager> GetAvailableManagers() {
        return _townHallManagers.Where(manager => manager.TownHall.IsOperational);
    }

    private IEnumerable<Unit> GetIdleWorkers() {
        return _workerDispatch
            .Where(dispatch => dispatch.Value == null)
            .Select(dispatch => dispatch.Key);
    }

    private IEnumerable<Unit> GetIdleQueens() {
        return _queenDispatch
            .Where(dispatch => dispatch.Value == null)
            .Select(dispatch => dispatch.Key);
    }

    private Color GetNewExpandColor() {
        // TODO GD Not very resilient, but simple enough for now
        return _expandColors[_townHallDispatch.Count % _expandColors.Count];
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

    // TODO GD This should increase for every macro hatch built?
    private static bool BankIsTooBig() {
        return Controller.AvailableMinerals > KnowledgeBase.GetUnitTypeData(Units.Hatchery).MineralCost * 2;
    }

    private bool HasReachedMaximumMacroTownHalls() {
        var nbTownHalls = _townHallDispatch.Count
                          + Controller.GetProducersCarryingOrders(Units.Hatchery).Count();

        return nbTownHalls >= Controller.GetMiningTownHalls().Count() * 2;
    }

    private static IEnumerable<Unit> GetTownHallsInConstruction() {
        return Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery)
            .Where(townHall => !townHall.IsOperational)
            .Concat(Controller.GetProducersCarryingOrders(Units.Hatchery));
    }
}
