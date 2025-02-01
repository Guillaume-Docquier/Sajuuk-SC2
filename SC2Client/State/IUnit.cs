using System.Numerics;
using Algorithms;
using SC2APIProtocol;
using SC2Client.PubSub;
using SC2Client.PubSub.Events;

namespace SC2Client.State;

/// <summary>
/// An interface representing a unit in the game.
/// Units are everything: actual units, buildings, resources, rocks, xel nagas, critters, etc.
/// </summary>
public interface IUnit : IHavePosition, IPublisher<UnitDeath> {
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

    /// <summary>
    /// Whether this unit is snapshot of the last time the unit was seen.
    /// </summary>
    public bool IsSnapshot { get; }

    public bool IsFlying { get; }
    public bool IsCloaked { get; }
    public bool IsBurrowed { get; }

    /// <summary>
    /// The frame at which the unit was last seen for real.
    /// </summary>
    public ulong LastSeen { get; }

    /// <summary>
    /// Gets the 2D distance between this unit and a 2D position.
    /// </summary>
    /// <param name="position">The position to get the 2D distance to</param>
    /// <returns></returns>
    public float Distance2DTo(Vector2 position);

    /// <summary>
    /// Gets the 2D distance between this unit and another one.
    /// </summary>
    /// <param name="otherUnit"></param>
    /// <returns></returns>
    public float Distance2DTo(IUnit otherUnit);
}
