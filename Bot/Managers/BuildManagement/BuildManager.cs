using System.Collections.Generic;
using Bot.Builds;

namespace Bot.Managers.BuildManagement;

public partial class BuildManager : Manager {
    public override IEnumerable<BuildFulfillment> BuildFulfillments { get; }
    protected override IAssigner Assigner { get; }
    protected override IDispatcher Dispatcher { get; }
    protected override IReleaser Releaser { get; }

    public BuildManager() {
        Assigner = new BuildManagerAssigner(this);
        Dispatcher = new BuildManagerDispatcher(this);
        Releaser = new BuildManagerReleaser(this);
    }

    protected override void AssignmentPhase() {
        throw new System.NotImplementedException();
    }

    protected override void DispatchPhase() {
        throw new System.NotImplementedException();
    }

    protected override void ManagementPhase() {
        throw new System.NotImplementedException();
    }
}
