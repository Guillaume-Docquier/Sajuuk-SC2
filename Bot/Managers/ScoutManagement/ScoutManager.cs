using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.Managers.ScoutManagement.ScoutingTasks;
using Bot.Managers.ScoutManagement.ScoutSupervision;
using Bot.MapKnowledge;

namespace Bot.Managers.ScoutManagement;

// TODO GD Do this for real. Supervisor and Scouting Tasks work.
public partial class ScoutManager : Manager {
    public override IEnumerable<BuildFulfillment> BuildFulfillments => Enumerable.Empty<BuildFulfillment>();

    protected override IAssigner Assigner { get; }
    protected override IDispatcher Dispatcher { get; }
    protected override IReleaser Releaser { get; }

    private readonly HashSet<ScoutSupervisor> _scoutSupervisors = new HashSet<ScoutSupervisor>();
    private readonly Dictionary<Region, bool> _scoutStatus = new Dictionary<Region, bool>();

    public ScoutManager() {
        Assigner = new ScoutManagerAssigner(this);
        Dispatcher = new ScoutManagerDispatcher(this);
        Releaser = new ScoutManagerReleaser(this);

        var initialScoutingTask = new RegionScoutingTask(MapAnalyzer.StartingLocation);
        _scoutSupervisors.Add(new ScoutSupervisor(initialScoutingTask));

        foreach (var region in RegionAnalyzer.Regions) {
            _scoutStatus[region] = false;
        }
    }

    protected override void AssignUnits() {
        Assign(Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Overlord).Where(unit => unit.Manager == null));
        Assign(Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Drone).Where(unit => unit.Manager == null));

        // Add some condition to request a Drone / Zergling
    }

    protected override void DispatchUnits() {
        // Create supervisors (scouting objectives)
        // Natural scouting with overlords
        // Drone scout enemy base
        // Proxy scout (with what?)
        // Over time scout for expands / army location

        Dispatch(ManagedUnits.Where(unit => unit.Supervisor == null));
    }

    protected override void Manage() {
        // Worker leaves at 1:00
        // Scout at 1:30, 2:30, 3:30, 4:30
        // Zerg expand first = ~1:00
        // Terran Protoss expand first = ~2:00
        if (_scoutSupervisors.Count <= 0) {
            return;
        }

        var completedTasks = _scoutSupervisors.Where(supervisor => supervisor.ScoutingTask.IsComplete()).ToList();
        foreach (var scoutSupervisor in completedTasks) {
            scoutSupervisor.Retire();
            _scoutSupervisors.Remove(scoutSupervisor);

            var scoutedRegion = scoutSupervisor.ScoutingTask.ScoutLocation.GetRegion();
            _scoutStatus[scoutedRegion] = true; // TODO GD We shouldn't have to know that it is a RegionScoutingTask

            foreach (var neighboringRegion in scoutedRegion.Neighbors) {
                if (!_scoutStatus[neighboringRegion.Region]) {
                    var newScoutingTask = new RegionScoutingTask(neighboringRegion.Region.Center);
                    _scoutSupervisors.Add(new ScoutSupervisor(newScoutingTask));
                }
            }
        }

        foreach (var scoutSupervisor in _scoutSupervisors) {
            scoutSupervisor.OnFrame();
        }
    }
}
