using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
            .Where(point => MapAnalyzer.IsWalkable(point, includeObstacles: false))
            .Select(point => point.ToVector2())
            .ToHashSet();

        Start = Edge.MinBy(edgePoint => edgePoint.DistanceTo(start.ToVector2()));
        End = Edge.MinBy(edgePoint => edgePoint.DistanceTo(end.ToVector2()));
        Length = Start.DistanceTo(End);
    }
}
