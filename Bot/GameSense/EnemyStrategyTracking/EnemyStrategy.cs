namespace Bot.GameSense.EnemyStrategyTracking;

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
