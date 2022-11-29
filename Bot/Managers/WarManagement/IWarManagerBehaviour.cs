using System.Collections.Generic;
using Bot.Builds;

namespace Bot.Managers.WarManagement;

public interface IWarManagerBehaviour {
    public IAssigner Assigner { get; }
    public IDispatcher Dispatcher { get; }
    public IReleaser Releaser { get; }

    public List<BuildRequest> BuildRequests { get; }

    public void RecruitmentPhase();
    public void DispatchPhase();
    public void ManagementPhase();

    // TODO GD This is not pretty, but for simplicity we'll roll with it
    public bool CleanUp();
}
