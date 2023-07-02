using System.Collections.Generic;
using Sajuuk.Builds;

namespace Sajuuk.Managers.WarManagement.States;

public interface IWarManagerBehaviour {
    public IAssigner Assigner { get; }
    public IDispatcher Dispatcher { get; }
    public IReleaser Releaser { get; }

    public List<BuildRequest> BuildRequests { get; }

    public void RecruitmentPhase();
    public void DispatchPhase();
    public void ManagementPhase();

    // TODO GD Put this in a separate interface!
    /// <summary>
    /// Do anything you need to clean up anything that was set up by the behaviour.
    /// Generally, that would be retiring all supervisors
    /// </summary>
    /// <returns>True if the cleanup is complete, false if you need more time.</returns>
    public bool CleanUp();
}
