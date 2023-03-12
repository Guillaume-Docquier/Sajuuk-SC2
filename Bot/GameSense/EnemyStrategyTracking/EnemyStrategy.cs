namespace Bot.GameSense.EnemyStrategyTracking;

/**
 * The possible enemy strategies
 * Each enum entry should be at most 20 characters to not be truncated when tagging the game with it.
 */
public enum EnemyStrategy {
    Unknown,

    // All races
    OneBase,
    WorkerRush,

    // Zerg
    TwelvePool,
    SixteenHatch,
    ZerglingRush,
    AggressivePool,

    // Terran
    FourRax,

    // Protoss
    CanonRush,
    FourGate,
}
