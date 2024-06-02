using System.Numerics;
using SC2Client.State;

namespace MapAnalysis.ExpandAnalysis;

/// <summary>
/// Represents an expand location located near a resource cluster.
/// </summary>
public interface IExpandLocation {
    /// <summary>
    /// The optimal position where you should build a townhall to collect the resources of this expand location.
    /// </summary>
    public Vector2 OptimalTownHallPosition { get; }

    /// <summary>
    /// The type of this expand location.
    /// </summary>
    public ExpandType ExpandType { get; }

    /// <summary>
    /// The set of resources present at this expand location.
    /// </summary>
    public HashSet<IUnit> Resources { get; }

    /// <summary>
    /// Whether the expand location's resources are depleted
    /// </summary>
    public bool IsDepleted { get; }

    /// <summary>
    /// Whether the expand position's optimal townhall position is obstructed.
    /// </summary>
    public bool IsObstructed { get; }
}
