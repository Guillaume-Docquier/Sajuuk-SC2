using System.Numerics;

namespace SC2Client.GameState;

/// <summary>
/// Provides information about the terrain.
/// </summary>
public interface ITerrain {
    /// <summary>
    /// Gets the height of the given cell.
    /// Cells in SC2 are 1x1. Any Vector2 within the same 1x1 cell will yield the same result.
    /// </summary>
    /// <param name="cell">The cell to query.</param>
    /// <returns>The height of the cell</returns>
    float GetHeight(Vector2 cell);

    /// <summary>
    /// Determines whether a cell can be walked on by a ground unit.
    /// Cells in SC2 are 1x1. Any Vector2 within the same 1x1 cell will yield the same result.
    /// The SC2 API data does not have the same resolution as the true in game data. Some cells can be considered unwalkable, but units can still walk on parts of that cell.
    /// As far as I know, walkable cells are always walkable.
    /// </summary>
    /// <param name="cell">The cell to query.</param>
    /// <param name="considerObstructions">Whether to consider obstructions. Obstructed cells are not walkable.</param>
    /// <returns>True if the cell can be walked on.</returns>
    bool IsWalkable(Vector2 cell, bool considerObstructions = true);

    /// <summary>
    /// Determines whether a cell can be built on.
    /// Cells in SC2 are 1x1. Any Vector2 within the same 1x1 cell will yield the same result.
    /// </summary>
    /// <param name="cell">The cell to query.</param>
    /// <param name="considerObstructions">Whether to consider obstructions. Obstructed cells are not buildable.</param>
    /// <returns>True if the cell can be built on.</returns>
    bool IsBuildable(Vector2 cell, bool considerObstructions = true);

    /// <summary>
    /// Determines whether a cell is obstructed by destructible neutral units.
    /// This will not consider ally or enemy buildings, only neutral units (rocks, minerals, etc) that can be destroyed.
    /// Cells in SC2 are 1x1. Any Vector2 within the same 1x1 cell will yield the same result.
    /// </summary>
    /// <param name="cell">The cell to query.</param>
    /// <returns>True if the cell is obstructed.</returns>
    bool IsObstructed(Vector2 cell);
}
