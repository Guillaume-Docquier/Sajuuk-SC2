namespace Bot.GameSense;

public interface IIncomeTracker {
    public float CurrentMineralsCollectionRate { get; }
    public float MaxMineralsCollectionRate { get; }
    public float AverageMineralsCollectionRate { get; }
    public float ExpectedMineralsCollectionRate { get; }

    public float CurrentVespeneCollectionRate { get; }
    public float MaxVespeneCollectionRate { get; }
    public float AverageVespeneCollectionRate { get; }
    public float ExpectedVespeneCollectionRate { get; }
}
