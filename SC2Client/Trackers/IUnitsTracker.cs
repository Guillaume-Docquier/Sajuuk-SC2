using SC2Client.State;

namespace SC2Client.Trackers;

public interface IUnitsTracker {
    /// <summary>
    /// The list of all visible and alive neutral units.
    /// </summary>
    IReadOnlyList<IUnit> NeutralUnits { get; }

    /// <summary>
    /// The list of all visible and alive owned units.
    /// </summary>
    IReadOnlyList<IUnit> OwnedUnits { get; }

    /// <summary>
    /// The list of all visible and alive enemy units.
    /// </summary>
    IReadOnlyList<IUnit> EnemyUnits { get; }
}
