using System.Numerics;
using System.Text.Json.Serialization;
using Algorithms.ExtensionMethods;
using SC2Client.Trackers;

namespace MapAnalysis.RegionAnalysis.ChokePoints;

public class ChokePoint {
    [JsonInclude] public Vector2 Start { get; private set; }
    [JsonInclude] public Vector2 End { get; private set; }
    [JsonInclude] public float Length { get; private set; }
    [JsonInclude] public HashSet<Vector2> Edge { get; private set; }

#pragma warning disable CS8618, CS9264
    [Obsolete("Do not use this parameterless JsonConstructor", error: true)]
    [JsonConstructor] public ChokePoint() {}
#pragma warning restore CS8618, CS9264

    public ChokePoint(Vector2 start, Vector2 end, ITerrainTracker terrainTracker) {
        Edge = start.GetCellsInBetween(end)
            .Where(cell => terrainTracker.Cells.Contains(cell))
            .ToHashSet();

        Start = Edge.MinBy(edgePoint => edgePoint.DistanceTo(start));
        End = Edge.MinBy(edgePoint => edgePoint.DistanceTo(end));
        Length = Start.DistanceTo(End);
    }
}
