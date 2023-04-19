namespace Bot.Managers;

/// <summary>
/// Manager that produces BuildFulfillments but doesn't want to control any units.
/// </summary>
public abstract class UnitlessManager : Manager {
    protected sealed override IAssigner Assigner { get; } = new DummyAssigner();
    protected sealed override IDispatcher Dispatcher { get; } = new DummyDispatcher();
    protected sealed override IReleaser Releaser { get; } = new DummyReleaser();

    protected sealed override void RecruitmentPhase() {}
    protected sealed override void DispatchPhase() {}

    private class DummyAssigner : IAssigner { public void Assign(Unit unit) {} }
    private class DummyDispatcher : IDispatcher { public void Dispatch(Unit unit) {} }
    private class DummyReleaser : IReleaser { public void Release(Unit unit) {} }
}
