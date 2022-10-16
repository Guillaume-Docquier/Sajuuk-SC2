using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.Managers.ScoutManagement.ScoutingTasks;

namespace Bot.Managers.ScoutManagement.ScoutSupervision;

public partial class ScoutSupervisor : Supervisor {
    public readonly ScoutingTask ScoutingTask;

    public override IEnumerable<BuildFulfillment> BuildFulfillments => Enumerable.Empty<BuildFulfillment>();
    protected override IAssigner Assigner { get; }
    protected override IReleaser Releaser { get; }

    public ScoutSupervisor(ScoutingTask scoutingTask) {
        Assigner = new ScoutSupervisorAssigner(this);
        Releaser = new ScoutSupervisorReleaser(this);

        ScoutingTask = scoutingTask;
    }

    protected override void Supervise() {
        ScoutingTask.Execute(SupervisedUnits);
    }

    public override void Retire() {
        foreach (var supervisedUnit in SupervisedUnits) {
            Release(supervisedUnit);
        }
    }
}
