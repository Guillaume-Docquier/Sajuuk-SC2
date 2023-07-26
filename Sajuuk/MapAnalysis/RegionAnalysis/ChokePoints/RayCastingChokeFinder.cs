using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sajuuk.ExtensionMethods;
using Sajuuk.Algorithms;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.GameSense;
using Sajuuk.Persistence;
using Sajuuk.Utils;
using SC2APIProtocol;

namespace Sajuuk.MapAnalysis.RegionAnalysis.ChokePoints;

// TODO GD Considering the obstacles (resources, rocks) might be interesting at some point
public class RayCastingChokeFinder : IChokeFinder {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IClustering _clustering;
    private readonly IMapImageFactory _mapImageFactory;
    private readonly string _mapFileName;

    private const bool DrawEnabled = true; // TODO GD Flag this

    private const float ChokeScoreCutOff = 4.4f;
    private const int StartingAngle = 0;
    private const int MaxAngle = 175;
    private const int AngleIncrement = 5;

    public RayCastingChokeFinder(
        ITerrainTracker terrainTracker,
        IGraphicalDebugger graphicalDebugger,
        IClustering clustering,
        IMapImageFactory mapImageFactory,
        string mapFileName
    ) {
        _terrainTracker = terrainTracker;
        _graphicalDebugger = graphicalDebugger;
        _clustering = clustering;
        _mapImageFactory = mapImageFactory;
        _mapFileName = mapFileName;
    }

    public List<ChokePoint> FindChokePoints() {
        var chokePointCells = CreateChokePointCells();
        Logger.Info($"Identified {chokePointCells.Count} potential choke point cells");

        var lines = CreateVisionLines(chokePointCells.Count);
        Logger.Info($"Computed {lines.Count} total vision lines");

        MarkTraversedChokePointCells(chokePointCells, lines);

        var chokePoints = ComputeChokePoints(chokePointCells);

        SaveChokePointsImage(chokePoints);

        return chokePoints;
    }

    /// <summary>
    /// Creates a dictionary of Vector2 -> ChokePointCell with all walkable cells in the map.
    /// The ChokePointCell will be used to compute choke scores.
    /// </summary>
    /// <returns>A mapping from a walkable Vector2 to a ChokePointCell.</returns>
    private Dictionary<Vector2, ChokePointCell> CreateChokePointCells() {
        var chokePointCells = new Dictionary<Vector2, ChokePointCell>();
        for (var x = 0; x < _terrainTracker.MaxX; x++) {
            for (var y = 0; y < _terrainTracker.MaxY; y++) {
                var position = new Vector2(x, y).AsWorldGridCenter();
                if (_terrainTracker.IsWalkable(position, includeObstacles: false)) {
                    chokePointCells[position] = new ChokePointCell(position);
                }
            }
        }

        return chokePointCells;
    }

    private List<VisionLine> CreateVisionLines(int chokePointCellsCount) {
        var allLines = new List<VisionLine>();
        for (var angle = StartingAngle; angle <= MaxAngle; angle += AngleIncrement) {
            var lines = CreateLinesAtAnAngle(angle, _terrainTracker.MaxX, _terrainTracker.MaxY);
            lines = BreakDownIntoContinuousSegments(lines);

            var nbCellsCovered = lines.SelectMany(line => line.OrderedTraversedCells).ToHashSet().Count;
            var percentageOfCellsCovered = (float)nbCellsCovered / chokePointCellsCount;
            Logger.Info($"Created {lines.Count,4} lines at {angle,3} degrees covering {nbCellsCovered,5} cells ({percentageOfCellsCovered,3:P0})");

            allLines.AddRange(lines);
        }

        Logger.Info("All vision lines computed!");

        return allLines;
    }

    private List<VisionLine> CreateLinesAtAnAngle(int angleInDegrees, int maxX, int maxY) {
        var origin = new Vector2 { X = maxX / 2f, Y = maxY / 2f };

        var lineLength = (int)Math.Ceiling(Math.Sqrt(maxX * maxX + maxY * maxY));
        var paddingX = (int)(lineLength / 2f - origin.X);
        var paddingY = (int)(lineLength / 2f - origin.Y);

        var angleInRadians = MathUtils.DegToRad(angleInDegrees);

        var lines = new List<VisionLine>();
        for (var y = -paddingY; y < maxY + paddingY; y++) {
            var start = new Vector2(-paddingX, y).RotateAround(origin, angleInRadians);
            var end = new Vector2(maxX + paddingX, y).RotateAround(origin, angleInRadians);

            lines.Add(new VisionLine(_terrainTracker, start, end, angleInDegrees));
        }

        return lines;
    }

    private List<VisionLine> BreakDownIntoContinuousSegments(IEnumerable<VisionLine> lines) {
        return lines.SelectMany(BreakDownIntoContinuousSegments).ToList();
    }

    private List<VisionLine> BreakDownIntoContinuousSegments(VisionLine visionLine) {
        var lines = new List<VisionLine>();

        var cellIndex = 0;
        while (cellIndex < visionLine.OrderedTraversedCells.Count) {
            var startCellIndex = GoToStartOfNextLine(visionLine, cellIndex);
            if (startCellIndex < 0) {
                break;
            }

            var endCellIndex = GoToEndOfLine(visionLine, startCellIndex);

            var cells = visionLine.OrderedTraversedCells
                .Skip(startCellIndex)
                .Take(endCellIndex - startCellIndex + 1)
                .ToList();

            lines.Add(new VisionLine(_terrainTracker, cells, visionLine.Angle));

            cellIndex = endCellIndex + 1;
        }

        return lines;
    }

    /// <summary>
    /// Iterate over the line's cells until we meet a walkable cell.
    /// Returns the index of that cell, or -1 if not found.
    /// </summary>
    /// <param name="visionLine">The line to iterate over</param>
    /// <param name="startCellIndex">The cell index to start at</param>
    /// <returns>The index of the next walkable cell or -1 if none</returns>
    private int GoToStartOfNextLine(VisionLine visionLine, int startCellIndex) {
        var currentCellIndex = startCellIndex;
        while (currentCellIndex < visionLine.OrderedTraversedCells.Count) {
            if (_terrainTracker.IsWalkable(visionLine.OrderedTraversedCells[currentCellIndex], includeObstacles: false)) {
                return currentCellIndex;
            }

            currentCellIndex++;
        }

        return -1;
    }

    /// <summary>
    /// Iterate over the line's cells until we meet an unwalkable cell.
    /// Returns the index of the last walkable cell encountered or -1 if none.
    /// </summary>
    /// <param name="visionLine">The line to iterate over</param>
    /// <param name="startCellIndex">The cell index to start at</param>
    /// <returns>The index of the last walkable cell or -1 if none</returns>
    private int GoToEndOfLine(VisionLine visionLine, int startCellIndex) {
        if (startCellIndex < 0) {
            return startCellIndex;
        }

        var currentCellIndex = startCellIndex;

        while (currentCellIndex < visionLine.OrderedTraversedCells.Count) {
            if (!_terrainTracker.IsWalkable(visionLine.OrderedTraversedCells[currentCellIndex], includeObstacles: false)) {
                return currentCellIndex - 1;
            }

            currentCellIndex++;
        }

        return currentCellIndex - 1;
    }

    private static void MarkTraversedChokePointCells(IReadOnlyDictionary<Vector2, ChokePointCell> chokePointCells, List<VisionLine> lines) {
        foreach (var line in lines) {
            foreach (var cell in line.OrderedTraversedCells) {
                chokePointCells[cell].VisionLines.Add(line);
            }
        }
    }

    private List<ChokePoint> ComputeChokePoints(Dictionary<Vector2, ChokePointCell> potentialChokePointCells) {
        foreach (var potentialChokePointCell in potentialChokePointCells.Values) {
            potentialChokePointCell.UpdateChokeScore();
        }

        LogChokeScoresDistribution(potentialChokePointCells.Values.Select(chokePointCell => chokePointCell.ChokeScore).ToList());

        var initialChokePointCells = potentialChokePointCells.Values.Where(chokePointCell => chokePointCell.ChokeScore > ChokeScoreCutOff).ToList();
        var (chokePointCellsClusters, _) = _clustering.DBSCAN(initialChokePointCells, 1.5f, 4);

        // Eliminate outliers from choke cells clusters.
        // We compute a dispersion score based on the average, median and std.
        // In low dispersion clusters, we are tolerant to outliers.
        // In high dispersion cluster, we are sensitive to outliers.
        var chokePointCells = new List<ChokePointCell>();
        foreach (var chokePointCellsCluster in chokePointCellsClusters) {
            var average = chokePointCellsCluster.Average(chokePointCell => chokePointCell.ChokeScore);
            var median = chokePointCellsCluster.OrderBy(chokePointCell => chokePointCell.ChokeScore).ToList()[chokePointCellsCluster.Count / 2].ChokeScore;
            var std = Math.Sqrt(chokePointCellsCluster.Average(chokePointCell => Math.Pow(chokePointCell.ChokeScore - average, 2)));

            // Dispersion is proportional to the relative std
            var dispersionScore = std / Math.Max(average, median);

            // Make high dispersion scores higher and small ones smaller
            dispersionScore = Math.Pow(dispersionScore + 0.5, 2) - 0.5;

            // Try to nullify the cut in low disparity clusters
            // A low cut means an inclusive cluster
            var cut = Math.Min(average, median) - (1 - dispersionScore) * std;

            var textGroup = new []
            {
                $"Avg: {average,4:F2}",
                $"Med: {median,4:F2}",
                $"Std: {std,4:F2}",
                $"Dsp: {dispersionScore,4:F2}",
                $"Cut: {cut,4:F2}",
            };
            var chokePointCellsClusterCenter = _terrainTracker.WithWorldHeight(_clustering.GetCenter(chokePointCellsCluster));
            _graphicalDebugger.AddTextGroup(textGroup, worldPos: chokePointCellsClusterCenter.ToPoint(zOffset: 5));
            _graphicalDebugger.AddLink(chokePointCellsClusterCenter, chokePointCellsClusterCenter.Translate(zTranslation: 5), Colors.SunbrightOrange);

            DebugScores(chokePointCellsCluster, cut);

            // Keep the cells that make the cut
            chokePointCells.AddRange(chokePointCellsCluster.Where(chokePointCell => chokePointCell.ChokeScore >= cut));
        }

        var chokeLines = chokePointCells
            .SelectMany(chokePointCell => chokePointCell.MostLikelyChokeLines)
            .GroupBy(line => line)
            .Where(group => group.Count() >= 2) // Some lines are length 0, some touch the same wall. This discards them but might also discard too many.
            .Where(group => group.Count() >= group.Key.Length * 0.5)
            .Select(group => group.Key)
            .ToList();

        DebugLines(chokeLines);

        var (lineCentersClusters, _) = _clustering.DBSCAN(chokeLines, 1.5f, 1);
        var chokePoints = new List<ChokePoint>();
        foreach (var lineCentersCluster in lineCentersClusters) {
            var clusterCenter = _clustering.GetCenter(lineCentersCluster);

            var shortestCenterLine = lineCentersCluster.MinBy(line => line.Length + line.Position.ToVector2().DistanceTo(clusterCenter) * 0.5)!;
            DebugLines(new List<VisionLine> { shortestCenterLine }, color: Colors.LimeGreen);
            chokePoints.Add(new ChokePoint(shortestCenterLine.Start, shortestCenterLine.End, _terrainTracker));
        }

        return chokePoints;
    }

    private void SaveChokePointsImage(IEnumerable<ChokePoint> chokePoints) {
        var mapImage = _mapImageFactory.CreateMapImage();

        var chokePointCells = chokePoints.SelectMany(chokePoint => chokePoint.Edge);
        foreach (var chokePointCell in chokePointCells) {
            mapImage.SetCellColor(chokePointCell, System.Drawing.Color.Lime);
        }

        mapImage.Save(FileNameFormatter.FormatDataFileName("ChokePoints", _mapFileName, "png"));
    }

    /// <summary>
    /// Displays the choke scores using the graphical debugger using a gradient from grey (low score) to red (high score).
    /// Cells that are under the given cutScore will be blue (not part of their choke cluster).
    /// </summary>
    /// <param name="chokePointCells">The cells to display the scores of.</param>
    /// <param name="cutScore">The minimum score to reach to be considered part of a choke point.</param>
    private void DebugScores(List<ChokePointCell> chokePointCells, double cutScore) {
        if (!Program.DebugEnabled || !DrawEnabled) {
            return;
        }

        var minScore = chokePointCells.Min(chokePointCell => chokePointCell.ChokeScore);
        var maxScore = chokePointCells.Max(chokePointCell => chokePointCell.ChokeScore);

        foreach (var chokePointCell in chokePointCells) {
            var textColor = Colors.Gradient(Colors.DarkGrey, Colors.DarkRed, MathUtils.LogScale(chokePointCell.ChokeScore, minScore, maxScore));
            if (chokePointCell.ChokeScore < cutScore) {
                textColor = Colors.Blue;
            }

            _graphicalDebugger.AddText($"{chokePointCell.ChokeScore:F1}", worldPos: _terrainTracker.WithWorldHeight(chokePointCell.Position).ToPoint(), color: textColor, size: 13);
        }
    }

    /// <summary>
    /// Displays the given VisionLines with the given color using the graphical debugger.
    /// </summary>
    /// <param name="lines">The lines to display.</param>
    /// <param name="color">The color to use. Defaults to Orange.</param>
    private void DebugLines(List<VisionLine> lines, Color color = null) {
        if (!Program.DebugEnabled || !DrawEnabled) {
            return;
        }

        foreach (var line in lines) {
            _graphicalDebugger.AddLink(
                _terrainTracker.WithWorldHeight(line.Start, zOffset: 0.5f),
                _terrainTracker.WithWorldHeight(line.End, zOffset: 0.5f),
                color ?? Colors.Orange
            );
        }
    }

    /// <summary>
    /// Logs the choke scores distribution to the console.
    /// The scores are grouped by their rounded value.
    /// </summary>
    /// <param name="chokeScores">The choke scores</param>
    private static void LogChokeScoresDistribution(List<float> chokeScores) {
        var minScore = (int)chokeScores.Min();
        var maxScore = (int)chokeScores.Max();

        var chokeScoresDistribution = new Dictionary<int, int>();
        for (var chokeScore = minScore; chokeScore <= maxScore; chokeScore++) {
            chokeScoresDistribution[chokeScore] = 0;
        }

        foreach (var chokeScore in chokeScores) {
            chokeScoresDistribution[(int)chokeScore] += 1;
        }

        Logger.Info("Choke score distribution");
        var cumulativeCount = 0;
        for (var chokeScore = minScore; chokeScore <= maxScore; chokeScore++) {
            if (chokeScoresDistribution[chokeScore] <= 0) {
                continue;
            }

            var count = chokeScoresDistribution[chokeScore];
            cumulativeCount += count;

            var percentage = (float)count / chokeScores.Count;
            var cumulativePercentage = (float)cumulativeCount / chokeScores.Count;
            Logger.Info($"{chokeScore,3}: {count,4} ({percentage,6:P2}) [{cumulativePercentage,6:P2}]");
        }
    }
}
