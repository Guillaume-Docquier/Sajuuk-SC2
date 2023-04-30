using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Bot.ExtensionMethods;
using Bot.GameSense;

namespace Bot.MapAnalysis.RegionAnalysis.ChokePoints;

public class ChokePoint {
    public Vector2 Start { get; }
    public Vector2 End { get; }
    public float Length { get; }
    public HashSet<Vector2> Edge { get; }

    public ChokePoint(Vector2 start, Vector2 end, ITerrainTracker terrainTracker) {
        Edge = start.GetPointsInBetween(end)
            .Where(cell => terrainTracker.IsWalkable(cell, includeObstacles: false))
            .ToHashSet();

        Start = Edge.MinBy(edgePoint => edgePoint.DistanceTo(start));
        End = Edge.MinBy(edgePoint => edgePoint.DistanceTo(end));
        Length = Start.DistanceTo(End);
    }

    /// <summary>
    /// This constructor is only meant for deserialization, you should not use it.
    /// </summary>
    [JsonConstructor]
    public ChokePoint(Vector2 start, Vector2 end, HashSet<Vector2> edge, float length) {
        Start = start;
        End = end;
        Edge = edge;
        Length = length;
    }
}
