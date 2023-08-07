using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Sajuuk.ExtensionMethods;
using Sajuuk.GameSense;

namespace Sajuuk.MapAnalysis.RegionAnalysis.ChokePoints;

public class ChokePoint {
    private static readonly float DiagonalDistance = (float)Math.Sqrt(1 + 1);

    [JsonInclude] public Vector2 Start { get; private set; }
    [JsonInclude] public Vector2 End { get; private set; }
    [JsonInclude] public float Length { get; private set; }
    [JsonInclude] public HashSet<Vector2> Edge { get; private set; }

    [JsonConstructor]
    [Obsolete("Do not use this parameterless JsonConstructor", error: true)]
    public ChokePoint() {}

    public ChokePoint(Vector2 start, Vector2 end, ITerrainTracker terrainTracker) {
        var edge = CreateLineWithoutDiagonals(start.GetPointsInBetween(end).ToList())
            .Where(cell => terrainTracker.IsWalkable(cell, considerObstaclesObstructions: false))
            .ToList();

        Edge = edge.ToHashSet();
        Start = edge.First();
        End = edge.Last();
        Length = Start.DistanceTo(End);
    }

    /// <summary>
    /// Creates a line without diagonally linked cells.
    /// </summary>
    /// <param name="roughLine">The original line that might contain diagonals.</param>
    /// <returns>A line where each cell is not diagonal to the next one.</returns>
    private static List<Vector2> CreateLineWithoutDiagonals(IReadOnlyList<Vector2> roughLine) {
        var line = new List<Vector2>();
        for (var i = 0; i < roughLine.Count - 1; i++) {
            var p1 = roughLine[i];
            var p2 = roughLine[i + 1];

            line.Add(p1);
            if (AreDiagonallyAcross(p1, p2)) {
                line.Add(p1.Translate(xTranslation: (p2 - p1).X).AsWorldGridCenter());
            }
        }

        line.Add(roughLine[^1]);

        return line;
    }

    /// <summary>
    /// Determines if two cells are diagonally across each other.
    /// </summary>
    /// <param name="cell1">The first cell.</param>
    /// <param name="cells2">The second cell.</param>
    /// <returns>True if the two cells are diagonally across each other</returns>
    private static bool AreDiagonallyAcross(Vector2 cell1, Vector2 cells2) {
        return Math.Abs(cell1.DistanceTo(cells2) - DiagonalDistance) < float.Epsilon;
    }
}
