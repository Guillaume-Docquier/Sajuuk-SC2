using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.Wrapper;

namespace Bot.MapKnowledge;

// TODO GD Considering the obstacles (resources, rocks) might be interesting at some point
public static partial class GridScanChokeFinder {
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

        ComputeChokeScores(nodes);

        return new List<ChokePoint>();
    }

    private static Dictionary<Vector3, Node> ComputeWalkableNodesInMap() {
        var nodes = new Dictionary<Vector3, Node>();
        for (var x = 0; x < MaxX; x++) {
            for (var y = 0; y < MaxY; y++) {
                var position = new Vector3(x, y, 0).AsWorldGridCenter();
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
        var lineLength = (int)Math.Ceiling(Math.Sqrt(MaxX * MaxX + MaxY * MaxY));
        var origin = new Vector3 { X = MaxX / 2f, Y = MaxY / 2f };
        var paddingX = (int)(lineLength / 2f - origin.X);
        var paddingY = (int)(lineLength / 2f - origin.Y);
        var angleInRadians = DegToRad(angleInDegrees);

        var lines = new List<VisionLine>();
        for (var y = -paddingY; y < MaxY + paddingY; y++) {
            var start = new Vector3(-paddingX, y, 0).Rotate2D(angleInRadians, origin);
            var end = new Vector3(MaxX + paddingX, y, 0).Rotate2D(angleInRadians, origin);

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

    private static void MarkTraversedNodes(IReadOnlyDictionary<Vector3, Node> nodes, List<VisionLine> lines) {
        foreach (var line in lines) {
            foreach (var cell in line.OrderedTraversedCells) {
                nodes[cell].VisionLines.Add(line);
            }
        }
    }

    private static void ComputeChokeScores(Dictionary<Vector3, Node> nodes) {
        foreach (var node in nodes.Values) {
            node.UpdateChokeScore();
        }

        foreach (var node in nodes.Values) {
            var neighbors = node.Position.GetReachableNeighbors(includeObstacles: false).Select(position => nodes[position]);
            node.UpdateChokeScoreDeltas(neighbors);
        }

        DebugScores(nodes.Values.Where(node => node.ChokeScore > 1.2f).Select(node => (node.Position, node.ChokeScore)).ToList());
    }

    private static void DebugScores(List<(Vector3 Position, float Score)> nodes) {
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

    private static void DebugLines(List<VisionLine> lines) {
        foreach (var line in lines) {
            Program.GraphicalDebugger.AddLink(line.Start.WithWorldHeight(), line.End.WithWorldHeight(), Colors.Orange, withText: false);
        }
    }
}
