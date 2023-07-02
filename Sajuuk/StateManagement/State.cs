namespace Sajuuk.StateManagement;

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
    protected TContext Context { get; private set; }

    public void SetStateMachine(StateMachine<TContext> stateMachine)
    {
        if (StateMachine == stateMachine) {
            return;
        }

        StateMachine = stateMachine;

        OnStateMachineSet();
    }

    public void SetContext(TContext context)
    {
        if (Context == context) {
            return;
        }

        Context = context;

        OnContextSet();
    }

    protected virtual void OnStateMachineSet() {}
    protected virtual void OnContextSet() {}

    public void OnFrame() {
        Execute();

        if (TryTransitioning()) {
            OnTransition();
        }
    }

    protected abstract void Execute();

    protected abstract bool TryTransitioning();

    protected virtual void OnTransition() {}
}

public abstract class State<TContext, TStateMachine>: State<TContext>
    where TContext: class
    where TStateMachine: StateMachine<TContext> {
    protected new TStateMachine StateMachine => base.StateMachine as TStateMachine;
}
