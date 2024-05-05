using Algorithms;
using SC2APIProtocol;

namespace SC2Client.State;

/// <summary>
/// An interface representing a unit in the game.
/// Units are everything: actual units, buildings, resources, rocks, xel nagas, critters, etc.
/// </summary>
public interface IUnit : IHavePosition {
    /// <summary>
    /// The unique identifier of the unit.
    /// </summary>
    public ulong Tag { get; }

    public string Name { get; }
    public uint UnitType { get; }
    public float FoodRequired { get; }

    /// <summary>
    /// The radius of the unit.
    /// Large units like ultralisks have a larger radius than small units like zerglings.
    /// </summary>
    public float Radius { get; }

    public Alliance Alliance { get; }
    public bool IsVisible { get; }
    public bool IsFlying { get; }
    public bool IsCloaked { get; }
    public bool IsBurrowed { get; }
    public ulong LastSeen { get; }
}
