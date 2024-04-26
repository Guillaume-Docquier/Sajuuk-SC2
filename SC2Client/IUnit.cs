using System.Numerics;
using SC2APIProtocol;

namespace SC2Client;

/// <summary>
/// An interface representing a unit in the game.
/// Units are everything: actual units, buildings, resources, rocks, xel nagas, critters, etc.
/// </summary>
public interface IUnit {
    public string Name { get; }
    public ulong Tag { get; }
    public uint UnitType { get; }
    public float FoodRequired { get; }
    public float Radius { get; }
    public Alliance Alliance { get; }
    public Vector3 Position { get; }
    public bool IsVisible { get; }
    public bool IsFlying { get; }
}
