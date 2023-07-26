using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Sajuuk.MapAnalysis.RegionAnalysis.ChokePoints;

public class ChokePointCell : IHavePosition {
    /// <summary>
    /// The choke point cell position.
    /// We would like a Vector2 here but IHavePosition requires Vector3.
    /// </summary>
    public Vector3 Position { get; }

    /// <summary>
    /// All the vision lines that go through this choke point cell.
    /// </summary>
    public List<VisionLine> VisionLines { get; } = new List<VisionLine>();

    /// <summary>
    /// The choke score for this choke point cell. The higher, the more likely this cell is part of a choke point.
    /// </summary>
    public float ChokeScore { get; private set; }

    /// <summary>
    /// The vision lines that go through this cell that are most likely part of a choke point.
    /// </summary>
    public List<VisionLine> MostLikelyChokeLines { get; private set; }

    public ChokePointCell(Vector2 position) {
        Position = new Vector3 { X = position.X, Y = position.Y, Z = 0 };
    }

    /// <summary>
    /// Updates the choke score of this cell based on the vision lines that go through the cell.
    /// We do this by scoring the lines that go through the node and taking the average score of the top 25%.
    /// See the LineScorers for the line scoring strategies.
    /// </summary>
    public void UpdateChokeScore() {
        var bestChokeLinesWithTheirScores = VisionLines
            .Select(line => (Line: line, Score: LineScorers.MinOfBothHalvesSquaredLineScore(this, line)))
            .OrderByDescending(group => group.Score)
            .Take(VisionLines.Count / 4)
            .ToList();

        MostLikelyChokeLines = bestChokeLinesWithTheirScores.Select(group => group.Line).ToList();
        ChokeScore = bestChokeLinesWithTheirScores.Average(group => group.Score);
    }
}
