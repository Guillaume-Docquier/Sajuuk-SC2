namespace Bot.StateManagement;

public abstract class StateMachine {
    protected State State { get; private set; }

    protected StateMachine(State state) {
        Logger.Info("{0} initialized with state {1}", GetType().Name, state.GetType().Name);
        State = state;
        State.SetStateMachine(this);
    }

    public void TransitionTo(State state)
    {
        Logger.Info("{0} transitioning from {1} to {2}", GetType().Name, State.GetType().Name, state.GetType().Name);
        State = state;
        State.SetStateMachine(this);
    }
}

public abstract class StateMachine<TState> : StateMachine where TState : State {
    protected new TState State => base.State as TState;

    protected StateMachine(TState state) : base(state) {}
}
