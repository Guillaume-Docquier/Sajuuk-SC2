namespace Bot.StateManagement;

public abstract class State<TContext> where TContext: class {
    protected string Name {
        get {
            if (StateMachine == null) {
                return $"{GetType().Name}";
            }

            return $"{StateMachine.GetType().Name} {GetType().Name}";
        }
    }

    protected StateMachine<TContext> StateMachine { get; private set; }

    public void SetStateMachine(StateMachine<TContext> stateMachine)
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

public abstract class State<TContext, TStateMachine>: State<TContext>
    where TContext: class
    where TStateMachine: StateMachine<TContext> {
    protected new TStateMachine StateMachine => base.StateMachine as TStateMachine;
}
