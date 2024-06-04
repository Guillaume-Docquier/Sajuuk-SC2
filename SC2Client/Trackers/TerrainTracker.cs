using System.Numerics;
using SC2Client.State;

namespace SC2Client.Trackers;

public class TerrainTracker : ITracker, ITerrainTracker {
    public void Update(IGameState gameState) {
        throw new NotImplementedException();
    }

    public ITerrain Terrain { get; }

    public Vector3 WithWorldHeight(Vector2 cell, float zOffset = 0) {
        throw new NotImplementedException();
    }

    public bool IsWalkable(Vector2 cell, bool considerObstructions = true) {
        throw new NotImplementedException();
    }

    public bool IsBuildable(Vector2 cell, bool considerObstructions = true) {
        throw new NotImplementedException();
    }

    public bool IsObstructed(Vector2 cell) {
        throw new NotImplementedException();
    }

    public bool IsWithinBounds(Vector2 position) {
        throw new NotImplementedException();
    }

    public IEnumerable<Vector2> GetReachableNeighbors(Vector2 cell, IReadOnlySet<Vector2>? potentialNeighbors = null, bool considerObstructions = true) {
        throw new NotImplementedException();
    }

    public Vector2 GetClosestWalkable(Vector2 position, int searchRadius = 8, HashSet<Vector2>? allowedCells = null) {
        throw new NotImplementedException();
    }
}
