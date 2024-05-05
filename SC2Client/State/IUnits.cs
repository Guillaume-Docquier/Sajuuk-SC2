namespace SC2Client.State;

/// <summary>
/// Provides game state data about the units.
/// </summary>
public interface IUnits {
    /// <summary>
    /// The tags of all dead units.
    /// </summary>
    IReadOnlySet<ulong> DeadUnitTags { get; }

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
