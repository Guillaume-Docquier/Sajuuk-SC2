using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Sajuuk.ExtensionMethods;
using Sajuuk.GameSense;

namespace Sajuuk.MapAnalysis.RegionAnalysis.ChokePoints;

public class ChokePoint {
    [JsonInclude] public Vector2 Start { get; private set; }
    [JsonInclude] public Vector2 End { get; private set; }
    [JsonInclude] public float Length { get; private set; }
    [JsonInclude] public HashSet<Vector2> Edge { get; private set; }

    [JsonConstructor]
    [Obsolete("Do not use this parameterless JsonConstructor", error: true)]
    public ChokePoint() {}

    public ChokePoint(Vector2 start, Vector2 end, ITerrainTracker terrainTracker) {
        Edge = start.GetPointsInBetween(end)
            .Where(cell => terrainTracker.IsWalkable(cell, considerObstaclesObstructions: false))
            .ToHashSet();

        Start = Edge.MinBy(edgePoint => edgePoint.DistanceTo(start));
        End = Edge.MinBy(edgePoint => edgePoint.DistanceTo(end));
        Length = Start.DistanceTo(End);
    }
}
