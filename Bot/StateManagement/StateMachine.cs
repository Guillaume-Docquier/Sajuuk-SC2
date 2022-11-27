namespace Bot.StateManagement;

public class StateMachine<TContext> where TContext : class {
    protected State<TContext> State;
    private readonly TContext _context;

    public StateMachine(TContext context, State<TContext> state) {
        _context = context;

        State = state;
        State.SetStateMachine(this);
        State.SetContext(_context);
        Logger.Info("{0} state machine initialized with state {1}", _context.GetType().Name, state.GetType().Name);
    }

    public void OnFrame() {
        State.OnFrame();
    }

    public void TransitionTo(State<TContext> state)
    {
        Logger.Info("{0} state machine transitioning from {1} to {2}", _context.GetType().Name, State.GetType().Name, state.GetType().Name);
        State = state;
        State.SetStateMachine(this);
        State.SetContext(_context);
    }
}

public class StateMachine<TContext, TState>: StateMachine<TContext>
    where TContext: class
    where TState: State<TContext>  {

    // Making it public here is a bit sketch
    // TODO GD Check the SneakAttackTactic to make State protected here
    public new TState State => base.State as TState;

    public StateMachine(TContext context, TState state) : base(context, state) {}
}
