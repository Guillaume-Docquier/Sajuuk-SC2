using System;
using System.Collections.Generic;
using System.Linq;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.Managers;

public class EconomyManager: IManager {
    private readonly List<MiningManager> _miningManagers = new List<MiningManager>();
    private readonly Dictionary<ulong, MiningManager> _workerDispatch = new Dictionary<ulong, MiningManager>();
    private readonly Dictionary<ulong, MiningManager> _townHallDispatch = new Dictionary<ulong, MiningManager>();
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
        // TODO GD Gather all idle workers
        DispatchWorkers(Controller.GetUnits(Controller.NewOwnedUnits, Units.Drone).ToList());

        // Order more workers

        // Execute managers
        _miningManagers.ForEach(manager => manager.OnFrame());
    }

    public void ReportUnitDeath(Unit deadUnit) {
        switch (deadUnit.UnitType) {
            case Units.Hatchery:
                var manager = _townHallDispatch[deadUnit.Tag];
                _workerDispatch
                    .Where(dispatch => dispatch.Value == manager)
                    .ToList()
                    .ForEach(dispatch => _workerDispatch.Remove(dispatch.Key));

                _townHallDispatch.Remove(deadUnit.Tag);
                _miningManagers.Remove(manager);
                break;
            case Units.Drone:
                _workerDispatch.Remove(deadUnit.Tag);
                break;
        }
    }

    private void ManageTownHalls(IEnumerable<Unit> townHalls) {
        foreach (var townHall in townHalls) {
            townHall.AddDeathWatcher(this);

            var miningManager = new MiningManager(townHall, _expandColors[_townHallDispatch.Count]); // TODO GD Not very resilient, but simple enough for now
            _townHallDispatch[townHall.Tag] = miningManager;
            _miningManagers.Add(miningManager);
        }
    }

    // TODO GD Try to dispatch to closest base if under ideal threshold
    private void DispatchWorkers(List<Unit> workers) {
        workers.ForEach(worker => worker.AddDeathWatcher(this));

        var dispatched = 0;

        foreach (var miningManager in _miningManagers) {
            dispatched += AssignWorkersToManager(workers.Skip(dispatched), miningManager, miningManager.IdealAvailableCapacity);
        }

        // TODO GD Saturate evenly
        foreach (var miningManager in _miningManagers) {
            dispatched += AssignWorkersToManager(workers.Skip(dispatched), miningManager, miningManager.SaturatedAvailableCapacity);
        }

        // Equalize over saturation
        var maxOverSaturation = _miningManagers.Select(miningManager => Math.Abs(miningManager.SaturatedAvailableCapacity)).Max();
        foreach (var miningManager in _miningManagers) {
            dispatched += AssignWorkersToManager(workers.Skip(dispatched), miningManager, maxOverSaturation - Math.Abs(miningManager.SaturatedAvailableCapacity));
        }

        // Over saturate evenly
        var managerRoundRobinIndex = 0;
        foreach (var worker in workers.Skip(dispatched)) {
            AssignWorkerToManager(worker, _miningManagers[managerRoundRobinIndex]);
            managerRoundRobinIndex = (managerRoundRobinIndex + 1) % _miningManagers.Count;
        }
    }

    private int AssignWorkerToManager(Unit worker, MiningManager miningManager) {
        return AssignWorkersToManager(new List<Unit> { worker }, miningManager, 1);
    }

    private int AssignWorkersToManager(IEnumerable<Unit> workers, MiningManager miningManager, int maxAmount) {
        var workersToDispatch = workers.Take(maxAmount).ToList();
        workersToDispatch.ForEach(workerToDispatch => _workerDispatch[workerToDispatch.Tag] = miningManager);
        miningManager.AssignWorkers(workersToDispatch);

        return workersToDispatch.Count;
    }
}
