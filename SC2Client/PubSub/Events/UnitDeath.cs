using SC2Client.State;

namespace SC2Client.PubSub.Events;

/// <summary>
/// An event representing a unit's death.
/// </summary>
public struct UnitDeath {
    /// <summary>
    /// The unit that died.
    /// </summary>
    public IUnit unit;
}
