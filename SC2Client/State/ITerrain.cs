using System.Numerics;

namespace SC2Client.State;

/// <summary>
/// Provides game state data about the terrain.
/// </summary>
public interface ITerrain {
    /// <summary>
    /// The map's width
    /// </summary>
    int MaxX { get; }

    /// <summary>
    /// The map's height
    /// </summary>
    int MaxY { get; }

    /// <summary>
    /// Contains the heights of all cells in the game.
    /// The cells as expressed as their corner.
    /// </summary>
    IReadOnlyDictionary<Vector2, float> CellHeights { get; }

    /// <summary>
    /// Contains all cells that are walkable.
    /// The cells as expressed as their corner.
    /// </summary>
    IReadOnlySet<Vector2> WalkableCells { get; }

    /// <summary>
    /// Contains all cells that are buildable.
    /// The cells as expressed as their corner.
    /// </summary>
    IReadOnlySet<Vector2> BuildableCells { get; }
}
