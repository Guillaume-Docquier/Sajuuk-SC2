using System.Collections.Generic;
using System.Numerics;
using Bot.ExtensionMethods;

namespace Bot.MapKnowledge;

public class ChokePoint {
    public Vector3 Start { get; }
    public Vector3 End { get; }
    public float Length { get; }
    public HashSet<Vector3> Edge { get; }

    public ChokePoint(Vector3 start, Vector3 end) {
        Start = start;
        End = end;
        Length = start.HorizontalDistanceTo(end);
        Edge = start.GetPointsInBetween(end);
    }
}
