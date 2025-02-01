using SC2Client.State;

namespace SC2Client.Trackers;

/// <summary>
///
/// </summary>
public class UnitsTracker : ITracker, IUnitsTracker {
    /// <summary>
    /// Workers disappear when going inside extractors for 1.415 seconds
    /// We'll change their death delay so that we don't think they're dead
    /// </summary>
    private static readonly ulong GasDeathDelay = TimeUtils.SecsToFrames(1.415f) + 5; // +5 frames just to be sure

    /// <summary>
    /// We put a death timer on the enemy units because if they die out of sight, we'll never know.
    /// </summary>
    private const int EnemyDeathTimerSeconds = 4 * 60;

    private IUnits _lastSeenUnitsState = null!;

    public IReadOnlyList<IUnit> NeutralUnits => _lastSeenUnitsState.NeutralUnits;
    public IReadOnlyList<IUnit> OwnedUnits => _lastSeenUnitsState.OwnedUnits;
    public IReadOnlyList<IUnit> EnemyUnits => _lastSeenUnitsState.EnemyUnits;

    public void Update(IGameState gameState) {
        _lastSeenUnitsState = gameState.Units;

        // TODO GD Manage death timers / delays
        // TODO GD Update unit states so that we can keep references
        // TODO GD Might need UnitsByTag
        // TODO GD Might need NewOwnedUnits
        // TODO GD Ghost and Memorized units, or should it be another tracker?
    }
}
