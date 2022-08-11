using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapKnowledge;
using Bot.UnitModules;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.Managers;

public class EconomyManager: IManager {
    private readonly List<TownHallManager> _townHallManagers = new List<TownHallManager>();
    private readonly Dictionary<Unit, TownHallManager> _townHallDispatch = new Dictionary<Unit, TownHallManager>();
    private readonly Dictionary<Unit, TownHallManager> _queenDispatch = new Dictionary<Unit, TownHallManager>();
    private readonly Dictionary<Unit, TownHallManager> _workerDispatch = new Dictionary<Unit, TownHallManager>();
    private readonly List<Color> _expandColors = new List<Color>
    {
        Colors.Maroon3,
        Colors.Burlywood,
        Colors.Cornflower,
        Colors.DarkGreen,
        Colors.DarkBlue,
    };

    private static readonly BuildOrders.BuildStep MacroHatchBuildRequest = new BuildOrders.BuildStep(BuildType.Build, 0, Units.Hatchery, 0);
    private static readonly BuildOrders.BuildStep QueenBuildRequest = new BuildOrders.BuildStep(BuildType.Train, 0, Units.Queen, 0);
    private readonly List<BuildOrders.BuildStep> _buildStepRequests = new List<BuildOrders.BuildStep>
    {
        MacroHatchBuildRequest,
        QueenBuildRequest,
    };

    public IEnumerable<BuildOrders.BuildStep> BuildStepRequests => _buildStepRequests.Concat(_townHallManagers.SelectMany(manager => manager.BuildStepRequests));

    public EconomyManager() {
        ManageTownHalls(Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery));
        DispatchWorkers(Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Drone).ToList());
    }

    public void OnFrame() {
        ManageTownHalls(Controller.GetUnits(UnitsTracker.NewOwnedUnits, Units.Hatchery));

        if (_townHallManagers.Count > 0) {
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

        // Build macro hatches
        if (ShouldBuildMacroHatch()) {
            MacroHatchBuildRequest.Quantity += 1;
            QueenBuildRequest.Quantity += 1;
        }

        QueenBuildRequest.Quantity += GetNumberOfMissingQueens();
    }

    public void Release(Unit unit) {
        if (_workerDispatch.TryGetValue(unit, out var townHallManager)) {
            townHallManager?.Release(unit);

            _workerDispatch.Remove(unit);
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
            var freeWorkers = managerWithExtraWorkers.ReleaseWorkers(nbWorkersToRelease);
            managerInNeed.AssignWorkers(freeWorkers.ToList());

            managerInNeed = GetClosestManagerWithIdealCapacityNotMet(MapAnalyzer.StartingLocation);
        }
    }

    private void DispatchQueens(List<Unit> queens) {
        foreach (var queen in queens) {
            queen.AddDeathWatcher(this);
            queen.Manager = this;

            var manager = GetClosestManagerWithNoQueen(queen);

            _queenDispatch[queen] = manager;

            if (manager != null) {
                manager.AssignQueen(queen);
            }
            else if (UnitModule.Get<QueenMicroModule>(queen) == null) {
                QueenMicroModule.Install(queen);
            }
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

    private bool ShouldBuildMacroHatch() {
        return MacroHatchBuildRequest.Quantity == 0
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
                          + Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Producers[Units.Hatchery]).Count(producer => producer.IsBuilding(Units.Hatchery))
                          + MacroHatchBuildRequest.Quantity;

        return nbTownHalls >= Controller.GetMiningTownHalls().Count() * 2;
    }

    private static IEnumerable<Unit> GetTownHallsInConstruction() {
        return Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery)
            .Where(townHall => !townHall.IsOperational)
            .Concat(Controller.GetUnitsInProduction(Units.Hatchery));
    }

    private static uint GetNumberOfMissingQueens() {
        var nbRequiredQueens = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery).Count() + 1;

        var nbQueens = Controller.GetUnitsInProduction(Units.Queen).Count()
                       + QueenBuildRequest.Quantity
                       + Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Queen).Count();

        return (uint)Math.Max(0, nbRequiredQueens - nbQueens);
    }
}
