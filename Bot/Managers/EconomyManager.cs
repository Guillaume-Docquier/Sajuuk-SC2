using System.Collections.Generic;
using System.Linq;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.Managers;

public class EconomyManager: IManager {
    private readonly List<MiningManager> _miningManagers = new List<MiningManager>();
    private readonly Dictionary<ulong, MiningManager> _workerDispatch = new Dictionary<ulong, MiningManager>();
    private readonly Dictionary<ulong, MiningManager> _baseDispatch = new Dictionary<ulong, MiningManager>();
    private readonly List<Color> _expandColors = new List<Color>
    {
        Colors.DarkGreen,
        Colors.DarkBlue,
        Colors.Maroon3,
        Colors.Burlywood,
        Colors.Cornflower,
    };

    public EconomyManager() {
        // Init drone dispatch
        var workers = Controller.GetUnits(Controller.OwnedUnits, Units.Drone).ToList();
        foreach (var worker in workers) {
            _workerDispatch[worker.Tag] = null;
        }

        // Init hatch dispatch and distribute drones
        var dispatched = 0;
        var bases = Controller.GetUnits(Controller.OwnedUnits, Units.Hatchery).ToList();
        foreach (var @base in bases) {
            var miningManager = new MiningManager(@base, _expandColors[_baseDispatch.Count]); // TODO GD Not very resilient, but simple enough for now
            _baseDispatch[@base.Tag] = miningManager;
            _miningManagers.Add(miningManager);

            var availableCapacity = miningManager.IdealAvailableCapacity;
            var workersToDispatch = workers.Skip(dispatched).Take(availableCapacity).ToList(); // TODO GD Is there a better way to do this?
            workersToDispatch.ForEach(worker => _workerDispatch[worker.Tag] = miningManager);
            miningManager.AssignWorkers(workersToDispatch);

            dispatched += availableCapacity;
        }
    }

    public void OnFrame() {
        // TODO GD Only select expand hatches, not macro hatches
        // Manage new hatcheries
        var newBases = Controller.GetUnits(Controller.NewOwnedUnits, Units.Hatchery).ToList();
        newBases.ForEach(@base => @base.AddDeathWatcher(this));
        foreach (var newBase in newBases) {
            var miningManager = new MiningManager(newBase, _expandColors[_baseDispatch.Count]); // TODO GD Not very resilient, but simple enough for now
            _baseDispatch[newBase.Tag] = miningManager;
            _miningManagers.Add(miningManager);
        }

        // TODO GD Remove extra drones from managers

        // Dispatch new drones
        var dispatched = 0;
        var newWorkers = Controller.GetUnits(Controller.NewOwnedUnits, Units.Drone).ToList();
        if (newWorkers.Any()) {
            newWorkers.ForEach(worker => worker.AddDeathWatcher(this));
            foreach (var miningManager in _miningManagers) {
                var availableCapacity = miningManager.IdealAvailableCapacity;
                var workersToDispatch = newWorkers.Skip(dispatched).Take(availableCapacity).ToList(); // TODO GD Is there a better way to do this?
                workersToDispatch.ForEach(worker => _workerDispatch[worker.Tag] = miningManager);
                miningManager.AssignWorkers(workersToDispatch);

                dispatched += availableCapacity;
            }
        }

        // Gather all idle drones?
        // Order more drones

        // Execute managers
        _miningManagers.ForEach(manager => manager.OnFrame());
    }

    public void ReportUnitDeath(Unit deadUnit) {
        switch (deadUnit.UnitType) {
            case Units.Hatchery:
                _baseDispatch.Remove(deadUnit.Tag);
                break;
            case Units.Drone:
                _workerDispatch.Remove(deadUnit.Tag);
                break;
        }
    }
}
