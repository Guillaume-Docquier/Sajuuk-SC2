namespace Bot.Managers.WarManagement.States;

public abstract class WarManagerStrategy : IStrategy {
    protected readonly WarManager WarManager;

    protected WarManagerStrategy(WarManager warManager) {
        WarManager = warManager;
    }

    public abstract void Execute();

    public virtual bool CanTransition() {
        return true;
    }
}
