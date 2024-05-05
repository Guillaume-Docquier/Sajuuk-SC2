namespace SC2Client.State;

/// <summary>
/// Provides game state data about the units.
/// </summary>
public interface IUnits {
    /// <summary>
    /// The list of all neutral units.
    /// The units will be updated as long as they remain visible, so you can keep a reference to them.
    /// </summary>
    public IReadOnlyList<IUnit> NeutralUnits { get; }

    /// <summary>
    /// The list of all owned units.
    /// The units will be updated as long as they remain visible, so you can keep a reference to them.
    /// </summary>
    public IReadOnlyList<IUnit> OwnedUnits { get; }

    /// <summary>
    /// The list of all enemy units.
    /// The units will be updated as long as they remain visible, so you can keep a reference to them.
    /// </summary>
    public IReadOnlyList<IUnit> EnemyUnits { get; }
}
