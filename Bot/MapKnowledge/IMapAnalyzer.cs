using System.Collections.Generic;
using System.Numerics;
using Bot.GameData;

namespace Bot.MapKnowledge;

public interface IMapAnalyzer {
    public bool IsInitialized { get; }
    public Vector2 StartingLocation { get; }
    public Vector2 EnemyStartingLocation { get; }

    public List<List<float>> HeightMap { get; }

    public int MaxX { get; }
    public int MaxY { get; }
    public float DiagonalLength { get; }

    public IReadOnlySet<Vector2> WalkableCells { get; }

    public float ExplorationRatio { get; }
    public float VisibilityRatio { get; }

    public string GetStartingCorner();

    public IEnumerable<Vector2> BuildSearchGrid(Vector2 centerPosition, int gridRadius, float stepSize = KnowledgeBase.GameGridCellWidth);
    public IEnumerable<Vector3> BuildSearchGrid(Vector3 centerPosition, int gridRadius, float stepSize = KnowledgeBase.GameGridCellWidth);

    public IEnumerable<Vector2> GetBuildingFootprint(Vector2 buildingCenter, uint buildingType);

    public IEnumerable<Vector2> BuildSearchRadius(Vector2 centerPosition, float circleRadius, float stepSize = KnowledgeBase.GameGridCellWidth);

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

    public HashSet<Vector3> GetPointsInBetween(Vector3 origin, Vector3 destination);
}
