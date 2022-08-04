namespace Bot.StateManagement;

public abstract class StateMachine {
    protected State State { get; private set; }

    protected StateMachine(State state) {
        State = state;
    }

    public void TransitionTo(State state)
    {
        Logger.Info("{0} transitioning to {1}",GetType().Name, state.GetType().Name);
        State = state;
        State.SetStateMachine(this);
    }
}
