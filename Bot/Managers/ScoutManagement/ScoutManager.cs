using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.Managers.ScoutManagement.ScoutingStrategies;
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

    private readonly IScoutingStrategy _scoutingStrategy;
    private readonly HashSet<ScoutSupervisor> _scoutSupervisors = new HashSet<ScoutSupervisor>();

    public ScoutManager() {
        Assigner = new ScoutManagerAssigner(this);
        Dispatcher = new ScoutManagerDispatcher(this);
        Releaser = new ScoutManagerReleaser(this);

        _scoutingStrategy = ScoutingStrategyFactory.CreateNew(Controller.EnemyRace);
    }

    protected override void AssignUnits() {
        Assign(Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Overlord).Where(unit => unit.Manager == null));

        // Add some condition to request a Drone / Zergling
    }

    protected override void DispatchUnits() {
        // Create supervisors (scouting objectives)
        // Natural scouting with overlords
        // Drone scout enemy base
        // Proxy scout (with what?)
        // Over time scout for expands / army location

        foreach (var scoutingTask in _scoutingStrategy.Execute()) {
            _scoutSupervisors.Add(new ScoutSupervisor(scoutingTask));
        }

        // TODO GD We might want to reassign as we go?
        Dispatch(ManagedUnits.Where(unit => unit.Supervisor == null));
    }

    protected override void Manage() {
        ClearCompletedTasks();

        // TODO GD Consider releasing units

        foreach (var scoutSupervisor in _scoutSupervisors) {
            scoutSupervisor.OnFrame();
        }
    }

    private void ClearCompletedTasks() {
        var completedTasks = _scoutSupervisors.Where(supervisor => supervisor.ScoutingTask.IsComplete());
        foreach (var scoutSupervisor in completedTasks) {
            scoutSupervisor.Retire();
            _scoutSupervisors.Remove(scoutSupervisor);
        }
    }
}
