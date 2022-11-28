namespace Bot.Managers.WarManagement.States;

public abstract class WarManagerStrategy : IStrategy {
    protected readonly WarManager WarManager;

    protected WarManagerStrategy(WarManager warManager) {
        WarManager = warManager;
    }

    /// <summary>
    /// Execute the strategy
    /// </summary>
    public abstract void Execute();

    /// <summary>
    /// Execute a cleanup sequence.
    /// Return true when all cleanup operations are complete.
    /// This is generally used when transitioning to another strategy.
    /// </summary>
    /// <returns>True if cleanup is done, false otherwise</returns>
    public abstract bool CleanUp();
}
