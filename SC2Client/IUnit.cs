using System.Numerics;
using Algorithms;
using SC2APIProtocol;

namespace SC2Client;

/// <summary>
/// An interface representing a unit in the game.
/// Units are everything: actual units, buildings, resources, rocks, xel nagas, critters, etc.
/// </summary>
public interface IUnit : IHavePosition {
    public string Name { get; }
    public ulong Tag { get; }
    public uint UnitType { get; }
    public float FoodRequired { get; }
    public float Radius { get; }
    public Alliance Alliance { get; }
    public bool IsVisible { get; }
    public bool IsFlying { get; }

    /// <summary>
    /// The unit's position on the ground.
    /// For ground units, this is the same as Position.
    /// For air units, the Z will be smaller than the Z of their Position.
    /// </summary>
    public Vector3 GroundPosition { get; }
}
