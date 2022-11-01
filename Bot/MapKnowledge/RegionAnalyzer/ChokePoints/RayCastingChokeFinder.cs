using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using SC2APIProtocol;

namespace Bot.MapKnowledge;

// TODO GD Considering the obstacles (resources, rocks) might be interesting at some point
public static partial class RayCastingChokeFinder {
    private const bool DrawEnabled = false; // TODO GD Flag this

    private const int StartingAngle = 0;
    private const int MaxAngle = 175;
    private const int AngleIncrement = 5;

    private static int MaxX => Controller.GameInfo.StartRaw.MapSize.X;
    private static int MaxY => Controller.GameInfo.StartRaw.MapSize.Y;

    public static List<ChokePoint> FindChokePoints() {
        var nodes = ComputeWalkableNodesInMap();
        Logger.Info("Computed {0} nodes", nodes.Count);

        var lines = CreateVisionLines(nodes.Count);
        Logger.Info("Computed {0} total vision lines", lines.Count);

        MarkTraversedNodes(nodes, lines);

        return ComputeChokePoints(nodes);
    }

    private static Dictionary<Vector2, Node> ComputeWalkableNodesInMap() {
        var nodes = new Dictionary<Vector2, Node>();
        for (var x = 0; x < MaxX; x++) {
            for (var y = 0; y < MaxY; y++) {
                var position = new Vector2(x, y).AsWorldGridCenter();
                if (MapAnalyzer.IsWalkable(position, includeObstacles: false)) {
                    nodes[position] = new Node(position);
                }
            }
        }

        return nodes;
    }

    private static List<VisionLine> CreateVisionLines(int nodeCount) {
        var savedVisionLines = VisionLinesDataStore.Load(Controller.GameInfo.MapName);
        if (savedVisionLines != null) {
            Logger.Info("Vision lines loaded from file!");
            return savedVisionLines;
        }

        var allLines = new List<VisionLine>();
        for (var angle = StartingAngle; angle <= MaxAngle; angle += AngleIncrement) {
            var lines = CreateLinesAtAnAngle(angle);
            lines = BreakDownIntoContinuousSegments(lines);

            var nbNodesCovered = lines.SelectMany(line => line.OrderedTraversedCells).ToHashSet().Count;
            Logger.Info("Created {0,4} lines at {1,3} degrees covering {2,5} nodes ({3,3:P0})", lines.Count, angle, nbNodesCovered, (float)nbNodesCovered / nodeCount);

            allLines.AddRange(lines);
        }

        VisionLinesDataStore.Save(Controller.GameInfo.MapName, allLines);
        Logger.Info("Vision lines saved!");

        return allLines;
    }

    private static List<VisionLine> CreateLinesAtAnAngle(int angleInDegrees) {
        var origin = new Vector2 { X = MaxX / 2f, Y = MaxY / 2f };

        var lineLength = (int)Math.Ceiling(Math.Sqrt(MaxX * MaxX + MaxY * MaxY));
        var paddingX = (int)(lineLength / 2f - origin.X);
        var paddingY = (int)(lineLength / 2f - origin.Y);

        var angleInRadians = DegToRad(angleInDegrees);

        var lines = new List<VisionLine>();
        for (var y = -paddingY; y < MaxY + paddingY; y++) {
            var start = new Vector2(-paddingX, y).Rotate(angleInRadians, origin);
            var end = new Vector2(MaxX + paddingX, y).Rotate(angleInRadians, origin);

            lines.Add(new VisionLine(start, end, angleInDegrees));
        }

        return lines;
    }

    private static double DegToRad(double degrees) {
        return Math.PI / 180 * degrees;
    }

    private static List<VisionLine> BreakDownIntoContinuousSegments(IEnumerable<VisionLine> lines) {
        return lines.SelectMany(BreakDownIntoContinuousSegments).ToList();
    }

    private static List<VisionLine> BreakDownIntoContinuousSegments(VisionLine visionLine) {
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

            lines.Add(new VisionLine(cells, visionLine.Angle));

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
    private static int GoToStartOfNextLine(VisionLine visionLine, int startCellIndex) {
        var currentCellIndex = startCellIndex;
        while (currentCellIndex < visionLine.OrderedTraversedCells.Count) {
            if (MapAnalyzer.IsWalkable(visionLine.OrderedTraversedCells[currentCellIndex], includeObstacles: false)) {
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
    private static int GoToEndOfLine(VisionLine visionLine, int startCellIndex) {
        if (startCellIndex < 0) {
            return startCellIndex;
        }

        var currentCellIndex = startCellIndex;

        while (currentCellIndex < visionLine.OrderedTraversedCells.Count) {
            if (!MapAnalyzer.IsWalkable(visionLine.OrderedTraversedCells[currentCellIndex], includeObstacles: false)) {
                return currentCellIndex - 1;
            }

            currentCellIndex++;
        }

        return currentCellIndex - 1;
    }

    private static void MarkTraversedNodes(IReadOnlyDictionary<Vector2, Node> nodes, List<VisionLine> lines) {
        foreach (var line in lines) {
            foreach (var cell in line.OrderedTraversedCells) {
                nodes[cell].VisionLines.Add(line);
            }
        }
    }

    private static List<ChokePoint> ComputeChokePoints(Dictionary<Vector2, Node> nodes) {

        foreach (var node in nodes.Values) {
            node.UpdateChokeScore();
        }

        LogDistribution(nodes.Values.Select(node => node.ChokeScore).ToList());

        var chokeNodes = nodes.Values.Where(node => node.ChokeScore > 5f).ToList();
        DebugScores(chokeNodes.Select(node => (node.Position, node.ChokeScore)).ToList());

        var chokeLines = chokeNodes
            .SelectMany(node => node.MostLikelyChokeLines)
            .GroupBy(line => line)
            .Where(group => group.Count() >= 3) // Some lines are length 0, some touch the same wall. This discards them but might also discard too many.
            .Where(group => group.Count() >= group.Key.Length * 0.5)
            .Select(group => group.Key)
            .ToList();

        DebugLines(chokeLines);

        var (lineCentersClusters, _) = Clustering.DBSCAN(chokeLines, 1.5f, 2);
        var chokePoints = new List<ChokePoint>();
        foreach (var lineCentersCluster in lineCentersClusters) {
            var clusterCenter = Clustering.GetCenter(lineCentersCluster);

            var shortestCenterLine = lineCentersCluster.MinBy(line => line.Length + line.Position.HorizontalDistanceTo(clusterCenter) * 0.5)!;
            DebugLines(new List<VisionLine> { shortestCenterLine }, color: Colors.LimeGreen);
            chokePoints.Add(new ChokePoint(shortestCenterLine.Start, shortestCenterLine.End));
        }

        return chokePoints;
    }

    private static void DebugScores(List<(Vector3 Position, float Score)> nodes) {
        if (!Program.DebugEnabled || !DrawEnabled) {
            return;
        }

        var minScore = nodes.Min(node => node.Score);
        var maxScore = nodes.Max(node => node.Score);

        foreach (var node in nodes) {
            var textColor = Colors.Gradient(Colors.DarkGrey, Colors.DarkRed, LogScale(node.Score, minScore, maxScore));
            Program.GraphicalDebugger.AddText($"{node.Score:F1}", worldPos: node.Position.WithWorldHeight().ToPoint(), color: textColor, size: 13);
        }
    }

    private static float LogScale(float number, float min, float max) {
        var logNum = (float)Math.Log2(number + 1);
        var logMin = (float)Math.Log2(min + 1);
        var logMax = (float)Math.Log2(max + 1);

        return (logNum - logMin) / (logMax - logMin);
    }

    private static float LinScale(float number, float min, float max) {
        return (number - min) / (max - min);
    }

    private static void DebugLines(List<VisionLine> lines, Color color = null) {
        if (!Program.DebugEnabled || !DrawEnabled) {
            return;
        }

        foreach (var line in lines) {
            Program.GraphicalDebugger.AddLink(line.Start.ToVector3(zOffset: 0.5f), line.End.ToVector3(zOffset: 0.5f), color ?? Colors.Orange, withText: false);
        }
    }

    private static void LogDistribution(List<float> scores) {
        var min = (int)scores.Min();
        var max = (int)scores.Max();

        var distribution = new Dictionary<int, int>();
        for (var bucketIndex = min; bucketIndex <= max; bucketIndex++) {
            distribution[bucketIndex] = 0;
        }

        foreach (var score in scores) {
            distribution[(int)score] += 1;
        }

        Logger.Info("Choke score distribution");
        var cumul = 0;
        for (var bucketIndex = min; bucketIndex <= max; bucketIndex++) {
            if (distribution[bucketIndex] > 0) {
                var count = distribution[bucketIndex];
                cumul += count;
                Logger.Info("{0,3}: {1,4} ({2,4:P2}) [{3,4:P2}]", bucketIndex, count, (float)count / scores.Count, (float)cumul / scores.Count);
            }
        }
    }
}
