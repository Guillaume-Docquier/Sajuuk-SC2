namespace Bot.GameSense.EnemyStrategyTracking;

public interface IEnemyStrategyTracker : IPublisher<EnemyStrategyTransition> {
    public EnemyStrategy CurrentEnemyStrategy { get; }
}
