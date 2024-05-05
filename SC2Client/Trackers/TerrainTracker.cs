using System.Numerics;

namespace SC2Client.Trackers;

public class TerrainTracker : ITerrainTracker {
    public Vector3 WithWorldHeight(Vector2 cell) {
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
}
