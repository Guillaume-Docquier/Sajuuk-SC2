namespace Bot.StateManagement;

public abstract class State {
    protected string Name {
        get {
            if (StateMachine == null) {
                return $"{GetType().Name}";
            }

            return $"{StateMachine.GetType().Name} {GetType().Name}";
        }
    }

    protected StateMachine StateMachine { get; private set; }

    public void SetStateMachine(StateMachine stateMachine)
    {
        StateMachine = stateMachine;

        OnSetStateMachine();
    }

    protected virtual void OnSetStateMachine() {}

    public void OnFrame() {
        Execute();

        if (TryTransitioning()) {
            OnTransition();
        }
    }

    protected abstract bool TryTransitioning();

    protected virtual void OnTransition() {}

    protected abstract void Execute();
}

public abstract class State<TStateMachine>: State where TStateMachine : StateMachine {
    protected new TStateMachine StateMachine => base.StateMachine as TStateMachine;
}
