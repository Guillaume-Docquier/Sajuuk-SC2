namespace Bot.Managers.WarManagement.States;

public abstract class Strategy<TContext> : IStrategy {
    protected readonly TContext Context;

    protected Strategy(TContext context) {
        Context = context;
    }

    public abstract void Execute();
}
