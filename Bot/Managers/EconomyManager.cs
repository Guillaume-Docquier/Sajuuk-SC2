using System;
using System.Collections.Generic;
using System.Linq;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.Managers;

public class EconomyManager: IManager {
    private readonly List<MiningManager> _miningManagers = new List<MiningManager>();
    private readonly Dictionary<Unit, MiningManager> _workerDispatch = new Dictionary<Unit, MiningManager>();
    private readonly Dictionary<Unit, MiningManager> _townHallDispatch = new Dictionary<Unit, MiningManager>();
    private readonly List<Color> _expandColors = new List<Color>
    {
        Colors.Maroon3,
        Colors.Burlywood,
        Colors.Cornflower,
        Colors.DarkGreen,
        Colors.DarkBlue,
    };

    public EconomyManager() {
        ManageTownHalls(Controller.GetUnits(Controller.OwnedUnits, Units.Hatchery));
        DispatchWorkers(Controller.GetUnits(Controller.OwnedUnits, Units.Drone).ToList());
    }

    public void OnFrame() {
        // TODO GD Only select expand hatches, not macro hatches
        ManageTownHalls(Controller.GetUnits(Controller.NewOwnedUnits, Units.Hatchery));

        // TODO GD Redistribute extra workers from managers
        var workersToDispatch = Controller.GetUnits(Controller.NewOwnedUnits, Units.Drone).ToList();
        workersToDispatch.AddRange(GetIdleWorkers());

        DispatchWorkers(workersToDispatch);

        // TODO GD Order more workers

        // Execute managers
        _miningManagers.ForEach(manager => manager.OnFrame());
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
                _miningManagers.Remove(manager);
                break;
            case Units.Drone:
                _workerDispatch.Remove(deadUnit);
                break;
        }
    }

    private void ManageTownHalls(IEnumerable<Unit> townHalls) {
        foreach (var townHall in townHalls) {
            townHall.AddDeathWatcher(this);

            var miningManager = new MiningManager(townHall, _expandColors[_townHallDispatch.Count]); // TODO GD Not very resilient, but simple enough for now
            _townHallDispatch[townHall] = miningManager;
            _miningManagers.Add(miningManager);
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

    private MiningManager GetClosestManagerWithIdealCapacityNotMet(Unit worker) {
        return _miningManagers
            .Where(manager => manager.IdealAvailableCapacity > 0)
            .OrderBy(manager => manager.TownHall.DistanceTo(worker))
            .FirstOrDefault();
    }

    private MiningManager GetClosestManagerWithSaturatedCapacityNotMet(Unit worker) {
        return _miningManagers
            .Where(manager => manager.SaturatedAvailableCapacity > 0)
            .OrderBy(manager => manager.TownHall.DistanceTo(worker))
            .FirstOrDefault();
    }

    private MiningManager GetManagerWithHighestAvailableCapacity() {
        return _miningManagers
            .OrderByDescending(manager => manager.SaturatedAvailableCapacity)
            .FirstOrDefault();
    }

    private IEnumerable<Unit> GetIdleWorkers() {
        return _workerDispatch
            .Where(dispatch => dispatch.Value == null)
            .Select(dispatch => dispatch.Key);
    }
}
