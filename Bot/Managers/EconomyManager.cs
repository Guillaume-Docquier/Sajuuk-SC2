using System;
using System.Collections.Generic;
using System.Linq;
using Bot.GameData;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.Managers;

public class EconomyManager: IManager {
    private const int MacroHatchBuildRequestIndex = 0;
    private const int QueenBuildRequestIndex = 1;

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

    private readonly List<BuildOrders.BuildStep> _buildStepRequests = new List<BuildOrders.BuildStep>
    {
        new BuildOrders.BuildStep(BuildType.Build, 0, Units.Hatchery, 0),
        new BuildOrders.BuildStep(BuildType.Train, 0, Units.Queen, 0),
    };

    public IEnumerable<BuildOrders.BuildStep> BuildStepRequests => _buildStepRequests.Concat(_townHallManagers.SelectMany(manager => manager.BuildStepRequests));

    public EconomyManager() {
        ManageTownHalls(Controller.GetUnits(Controller.OwnedUnits, Units.Hatchery));
        DispatchWorkers(Controller.GetUnits(Controller.OwnedUnits, Units.Drone).ToList());
    }

    public void OnFrame() {
        ManageTownHalls(Controller.GetUnits(Controller.NewOwnedUnits, Units.Hatchery));

        // TODO GD Redistribute extra workers from managers
        var workersToDispatch = Controller.GetUnits(Controller.NewOwnedUnits, Units.Drone).ToList();
        workersToDispatch.AddRange(GetIdleWorkers());
        DispatchWorkers(workersToDispatch);

        EqualizeWorkers();

        var queensToDispatch = Controller.GetUnits(Controller.NewOwnedUnits, Units.Queen).ToList();
        queensToDispatch.AddRange(GetIdleQueens());
        DispatchQueens(queensToDispatch);

        // Execute managers
        _townHallManagers.ForEach(manager => manager.OnFrame());

        // Build macro hatches
        if (ShouldBuildMacroHatch()) {
            _buildStepRequests[MacroHatchBuildRequestIndex].Quantity += 1;
            _buildStepRequests[QueenBuildRequestIndex].Quantity += 1;
        }
    }

    public void Retire() {
        throw new System.NotImplementedException();
    }

    public void ReportUnitDeath(Unit deadUnit) {
        switch (deadUnit.UnitType) {
            case Units.Hatchery:
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

            var miningManager = new TownHallManager(townHall, GetNewExpandColor());
            _townHallDispatch[townHall] = miningManager;
            _townHallManagers.Add(miningManager);
        }
    }

    private void DispatchWorkers(List<Unit> workers) {
        foreach (var worker in workers) {
            worker.AddDeathWatcher(this);

            var manager = GetClosestManagerWithIdealCapacityNotMet(worker);
            manager ??= GetClosestManagerWithSaturatedCapacityNotMet(worker);
            manager ??= GetManagerWithHighestAvailableCapacity();

            _workerDispatch[worker] = manager;
            manager.AssignWorker(worker);
        }
    }

    private void EqualizeWorkers() {
        var managerInNeed = GetClosestManagerWithIdealCapacityNotMet(Controller.StartingTownHall);
        while (managerInNeed != null) {
            var requiredWorkers = managerInNeed.IdealAvailableCapacity;
            var managerWithExtraWorkers = _townHallManagers.FirstOrDefault(manager => manager.IdealAvailableCapacity < 0); // Negative IdealAvailableCapacity means they have extra workers
            if (managerWithExtraWorkers == null) {
                break;
            }

            var nbWorkersToRelease = Math.Min(-1 * managerWithExtraWorkers.IdealAvailableCapacity, requiredWorkers);
            var freeWorkers = managerWithExtraWorkers.ReleaseWorkers(nbWorkersToRelease);
            managerInNeed.AssignWorkers(freeWorkers.ToList());

            managerInNeed = GetClosestManagerWithIdealCapacityNotMet(Controller.StartingTownHall);
        }
    }

    private void DispatchQueens(List<Unit> queens) {
        foreach (var queen in queens) {
            queen.AddDeathWatcher(this);

            var manager = GetClosestManagerWithNoQueen(queen);

            _queenDispatch[queen] = manager;
            manager?.AssignQueen(queen);
        }
    }

    private TownHallManager GetClosestManagerWithIdealCapacityNotMet(Unit worker) {
        return GetAvailableManagers()
            .Where(manager => manager.IdealAvailableCapacity > 0)
            .MinBy(manager => manager.TownHall.DistanceTo(worker));
    }

    private TownHallManager GetClosestManagerWithSaturatedCapacityNotMet(Unit worker) {
        return GetAvailableManagers()
            .Where(manager => manager.SaturatedAvailableCapacity > 0)
            .MinBy(manager => manager.TownHall.DistanceTo(worker));
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

    private IEnumerable<TownHallManager> GetUnavailableManagers() {
        return _townHallManagers.Where(manager => !manager.TownHall.IsOperational);
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
        return _buildStepRequests[MacroHatchBuildRequestIndex].Quantity == 0
               && BankIsTooBig()
               && !GetIdleLarvae().Any()
               && !HasEnoughMacroTownHalls()
               && !GetTownHallsInConstruction().Any();
    }

    private static IEnumerable<Unit> GetIdleLarvae() {
        return Controller.GetUnits(Controller.OwnedUnits, Units.Larva)
            .Where(larva => !larva.Orders.Any());
    }

    private static bool BankIsTooBig() {
        return Controller.AvailableMinerals > KnowledgeBase.GetUnitTypeData(Units.Hatchery).MineralCost * 2;
    }

    private bool HasEnoughMacroTownHalls() {
        var nbTownHalls = _townHallDispatch.Count
                          + Controller.GetUnits(Controller.OwnedUnits, Units.Producers[Units.Hatchery]).Count(producer => producer.IsBuilding(Units.Hatchery))
                          + _buildStepRequests[MacroHatchBuildRequestIndex].Quantity;

        return nbTownHalls >= Controller.GetMiningTownHalls().Count() * 2;
    }

    private static IEnumerable<Unit> GetTownHallsInConstruction() {
        return Controller.GetUnits(Controller.OwnedUnits, Units.Hatchery)
            .Where(townHall => !townHall.IsOperational)
            .Concat(Controller.GetUnitsInProduction(Units.Hatchery));
    }
}
