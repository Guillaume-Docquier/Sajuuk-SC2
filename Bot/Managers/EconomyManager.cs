using System.Collections.Generic;
using System.Linq;

namespace Bot.Managers;

public class EconomyManager: IManager {
    private readonly List<MiningManager> _miningManagers = new List<MiningManager>();
    private Dictionary<ulong, MiningManager> _droneDispatch = new Dictionary<ulong, MiningManager>();
    private Dictionary<ulong, MiningManager> _hatcheryDispatch = new Dictionary<ulong, MiningManager>();

    public EconomyManager() {
        // Init drone dispatch
        var drones = Controller.GetUnits(Controller.OwnedUnits, Units.Drone).ToList();
        foreach (var drone in drones) {
            _droneDispatch[drone.Tag] = null;
        }

        // Init hatch dispatch and distribute drones
        var dispatched = 0;
        var hatcheries = Controller.GetUnits(Controller.OwnedUnits, Units.Hatchery).ToList();
        foreach (var hatchery in hatcheries) {
            var miningManager = new MiningManager(hatchery);
            _hatcheryDispatch[hatchery.Tag] = miningManager;
            _miningManagers.Add(miningManager);

            var availableCapacity = miningManager.IdealAvailableCapacity;
            var dronesToDispatch = drones.Skip(dispatched).Take(availableCapacity).ToList(); // TODO GD Is there a better way to do this?
            dronesToDispatch.ForEach(drone => _droneDispatch[drone.Tag] = miningManager);
            miningManager.AssignWorkers(dronesToDispatch);

            dispatched += availableCapacity;
        }
    }

    public void OnFrame() {
        // TODO GD Only select expand hatches, not macro hatches
        // Manage new hatcheries
        var newHatcheries = Controller.GetUnits(Controller.NewOwnedUnits, Units.Hatchery);
        foreach (var newHatchery in newHatcheries) {
            var miningManager = new MiningManager(newHatchery);
            _hatcheryDispatch[newHatchery.Tag] = miningManager;
            _miningManagers.Add(miningManager);
        }

        // TODO GD Remove extra drones from managers

        // Dispatch new drones
        var dispatched = 0;
        var newDrones = Controller.GetUnits(Controller.NewOwnedUnits, Units.Drone).ToList();
        foreach (var miningManager in _miningManagers) {
            var availableCapacity = miningManager.IdealAvailableCapacity;
            var dronesToDispatch = newDrones.Skip(dispatched).Take(availableCapacity).ToList(); // TODO GD Is there a better way to do this?
            dronesToDispatch.ForEach(drone => _droneDispatch[drone.Tag] = miningManager);
            miningManager.AssignWorkers(dronesToDispatch);

            dispatched += availableCapacity;
        }

        // Gather all idle drones?
        // Order more drones

        // Execute managers
        _miningManagers.ForEach(supervisor => supervisor.OnFrame());
    }
}
