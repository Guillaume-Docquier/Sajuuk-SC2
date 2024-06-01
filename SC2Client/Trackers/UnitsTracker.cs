using SC2Client.State;

namespace SC2Client.Trackers;

public class UnitsTracker : ITracker, IUnitsTracker {
    /// <summary>
    /// Workers disappear when going inside extractors for 1.415 seconds
    /// We'll change their death delay so that we don't think they're dead
    /// </summary>
    private static readonly ulong GasDeathDelay = TimeUtils.SecsToFrames(1.415f) + 5; // +5 just to be sure

    /// <summary>
    /// We delay the death of enemy units because if they die out of sight, we'll never know.
    /// </summary>
    private const int EnemyDeathDelaySeconds = 4 * 60;

    public void Update(IGameState gameState) {
        throw new NotImplementedException();
    }

    public IReadOnlyList<IUnit> NeutralUnits => throw new NotImplementedException();
    public IReadOnlyList<IUnit> OwnedUnits => throw new NotImplementedException();
    public IReadOnlyList<IUnit> EnemyUnits => throw new NotImplementedException();
}
