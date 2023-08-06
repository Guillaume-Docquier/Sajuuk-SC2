using System.Collections.Generic;
using System.Numerics;
using Sajuuk.GameData;

namespace Sajuuk.GameSense;

public interface ITerrainTracker {
    public Vector2 StartingLocation { get; } // TODO GD Should this be here?
    public Vector2 EnemyStartingLocation { get; } // TODO GD Should this be here?
    public string GetStartingCorner(); // TODO GD Should this be here?

    /// <summary>
    /// Represents all cells that were walkable when the game started.
    /// TODO This definition is bad, some rocks can fall and block movements, making this unreliable
    /// </summary>
    public IReadOnlySet<Vector2> WalkableCells { get; }

    /// <summary>
    /// Represents all cells that could be walked on.
    /// This is essentially any cell that has terrain.
    /// </summary>
    public IReadOnlySet<Vector2> PlayableCells { get; }

    /// <summary>
    /// Represents all cells that are obstructed by neutral units (rocks, minerals, gas)
    /// </summary>
    public IReadOnlySet<Vector2> ObstructedCells { get; }

    public int MaxX { get; } // TODO GD Should this be here?
    public int MaxY { get; } // TODO GD Should this be here?
    public float DiagonalLength { get; } // TODO GD Should this be here?

    public float ExplorationRatio { get; } // TODO GD Should this be here?
    public float VisibilityRatio { get; } // TODO GD Should this be here?

    public IEnumerable<Vector2> BuildSearchGrid(Vector2 centerPosition, int gridRadius, float stepSize = KnowledgeBase.GameGridCellWidth); // TODO GD Should this be here?
    public IEnumerable<Vector3> BuildSearchGrid(Vector3 centerPosition, int gridRadius, float stepSize = KnowledgeBase.GameGridCellWidth); // TODO GD Should this be here?

    public IEnumerable<Vector2> GetBuildingFootprint(Vector2 buildingCenter, uint buildingType); // TODO GD Should this be here?

    public IEnumerable<Vector2> BuildSearchRadius(Vector2 centerPosition, float circleRadius, float stepSize = KnowledgeBase.GameGridCellWidth); // TODO GD Should this be here?

    public bool IsInBounds(Vector2 position);
    public bool IsInBounds(Vector3 position);
    public bool IsInBounds(float x, float y);

    public bool IsWalkable(Vector3 position, bool considerObstaclesObstructions = true);
    public bool IsWalkable(Vector2 position, bool considerObstaclesObstructions = true);

    public bool IsBuildable(Vector3 position, bool considerObstaclesObstructions = true);
    public bool IsBuildable(Vector2 position, bool considerObstaclesObstructions = true);

    public IEnumerable<Vector2> GetReachableNeighbors(Vector2 position, IReadOnlySet<Vector2> potentialNeighbors = null, bool considerObstaclesObstructions = true);

    public Vector2 GetClosestWalkable(Vector2 position, int searchRadius = 8, HashSet<Vector2> allowedCells = null);
    public Vector3 GetClosestWalkable(Vector3 position);

    public Vector3 WithWorldHeight(Vector2 vector, float zOffset = 0);
    public Vector3 WithWorldHeight(Vector3 vector, float zOffset = 0);

    public HashSet<Vector3> GetPointsInBetween(Vector3 origin, Vector3 destination); // TODO GD Should this be here?
}
