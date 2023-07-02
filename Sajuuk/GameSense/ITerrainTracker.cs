using System.Collections.Generic;
using System.Numerics;
using Sajuuk.GameData;

namespace Sajuuk.GameSense;

public interface ITerrainTracker {
    public Vector2 StartingLocation { get; } // TODO GD Should this be here?
    public Vector2 EnemyStartingLocation { get; } // TODO GD Should this be here?
    public string GetStartingCorner(); // TODO GD Should this be here?

    public List<List<float>> HeightMap { get; }
    public IReadOnlySet<Vector2> WalkableCells { get; }

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

    public bool IsWalkable(Vector3 position, bool includeObstacles = true);
    public bool IsWalkable(Vector2 position, bool includeObstacles = true);

    public bool IsBuildable(Vector3 position, bool includeObstacles = true);
    public bool IsBuildable(Vector2 position, bool includeObstacles = true);

    public IEnumerable<Vector2> GetReachableNeighbors(Vector2 position, bool includeObstacles = true);

    public Vector2 GetClosestWalkable(Vector2 position, int searchRadius = 8, HashSet<Vector2> allowedCells = null);
    public Vector3 GetClosestWalkable(Vector3 position);

    public Vector3 WithWorldHeight(Vector2 vector, float zOffset = 0);
    public Vector3 WithWorldHeight(Vector3 vector, float zOffset = 0);

    public HashSet<Vector3> GetPointsInBetween(Vector3 origin, Vector3 destination); // TODO GD Should this be here?
}
