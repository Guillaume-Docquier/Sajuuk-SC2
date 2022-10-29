using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Bot.ExtensionMethods;

namespace Bot.MapKnowledge;

public class ChokePoint {
    public Vector2 Start { get; }
    public Vector2 End { get; }
    public float Length { get; }
    public HashSet<Vector2> Edge { get; }

    public ChokePoint(Vector3 start, Vector3 end) {
        Edge = start.WithoutZ()
            .GetPointsInBetween(end.WithoutZ())
            .Where(cell => MapAnalyzer.IsWalkable(cell, includeObstacles: false))
            .Select(cell => cell.ToVector2())
            .ToHashSet();

        Start = Edge.MinBy(edgePoint => edgePoint.DistanceTo(start.ToVector2()));
        End = Edge.MinBy(edgePoint => edgePoint.DistanceTo(end.ToVector2()));
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
