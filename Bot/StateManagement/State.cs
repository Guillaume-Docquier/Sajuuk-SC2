namespace Bot.StateManagement;

public abstract class State {
    protected StateMachine StateMachine { get; private set; }

    public void SetStateMachine(StateMachine stateMachine)
    {
        StateMachine = stateMachine;

        OnSetStateMachine();
    }

    protected virtual void OnSetStateMachine() {}

    public void OnFrame() {
        if (TryTransitioning()) {
            OnTransition();

            return;
        }

        Execute();
    }

    protected abstract bool TryTransitioning();

    protected virtual void OnTransition() {}

    protected abstract void Execute();
}

public abstract class State<TStateMachine> : State where TStateMachine : StateMachine {
    protected new TStateMachine StateMachine => base.StateMachine as TStateMachine;
}
