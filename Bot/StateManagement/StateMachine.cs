namespace Bot.StateManagement;

public class StateMachine<TContext> where TContext : class {
    public State<TContext> State;

    public TContext Context { get; }

    public StateMachine(TContext context, State<TContext> state) {
        Context = context;

        State = state;
        State.SetStateMachine(this);
        Logger.Info("{0} state machine initialized with state {1}", Context.GetType().Name, state.GetType().Name);
    }

    public void OnFrame() {
        State.OnFrame();
    }

    public void TransitionTo(State<TContext> state)
    {
        Logger.Info("{0} state machine transitioning from {1} to {2}", Context.GetType().Name, State.GetType().Name, state.GetType().Name);
        State = state;
        State.SetStateMachine(this);
    }
}

public class StateMachine<TContext, TState>: StateMachine<TContext>
    where TContext: class
    where TState: State<TContext>  {
    public new TState State => base.State as TState;

    public StateMachine(TContext context, TState state) : base(context, state) {}
}
