using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.Wrapper;

namespace Bot.MapKnowledge;

// TODO GD Considering the obstacles (resources, rocks) might be interesting at some point
public static class GridScanChokeFinder {
    private const int StartingAngle = 0;
    private const int MaxAngle = 175;
    private const int AngleIncrement = 5;

    private static int MaxX => Controller.GameInfo.StartRaw.MapSize.X;
    private static int MaxY => Controller.GameInfo.StartRaw.MapSize.Y;

    private class Line {
        public List<Vector3> OrderedTraversedCells { get; }
        public float Angle { get; }

        public Vector3 Start { get; }
        public Vector3 End { get; }
        public float Length { get; }

        public Line(Vector3 start, Vector3 end, float angle) {
            var centerOfStart = start.AsWorldGridCenter();

            OrderedTraversedCells = start.GetPointsInBetween(end)
                .OrderBy(current => current.HorizontalDistanceTo(centerOfStart))
                .ToList();

            Start = OrderedTraversedCells[0];
            End = OrderedTraversedCells.Last();
            Length = Start.HorizontalDistanceTo(End);

            Angle = angle;
        }

        public Line(List<Vector3> orderedTraversedCells, float angle) {
            OrderedTraversedCells = orderedTraversedCells;

            Start = OrderedTraversedCells[0];
            End = OrderedTraversedCells.Last();
            Length = Start.HorizontalDistanceTo(End);

            Angle = angle;
        }
    }

    private class Node : IHavePosition {
        public Vector3 Position { get; }
        public List<Line> Lines { get; } = new List<Line>();

        public Line ShortestLine { get; private set; }

        public Node(Vector3 position) {
            Position = position;
        }

        public void UpdateShortestLine() {
            ShortestLine = Lines.MinBy(line => line.Length);
        }
    }

    public static List<ChokePoint> FindChokePoints() {
        var nodes = ComputeWalkableNodesInMap();
        Logger.Info("Computed {0} nodes", nodes.Count);

        for (var angle = StartingAngle; angle <= MaxAngle; angle += AngleIncrement) {
            var lines = CreateLines(angle);
            lines = BreakDown(lines);

            foreach (var line in lines) {
                // TODO GD Store cell index with the line for faster lookup later on
                for (var cellIndex = 0; cellIndex < line.OrderedTraversedCells.Count; cellIndex++) {
                    nodes[line.OrderedTraversedCells[cellIndex]].Lines.Add(line);
                }
            }

            var nbNodesAffected = lines.SelectMany(line => line.OrderedTraversedCells).ToHashSet().Count;
            Logger.Info("Created {0,4} lines at {1,3} degrees affecting {2,5} nodes ({3,3:P0})", lines.Count, angle, nbNodesAffected, (float)nbNodesAffected / nodes.Count);
        }

        foreach (var node in nodes.Values) {
            node.UpdateShortestLine();
        }

        var uniqueShortestLines = nodes.Values
            .Select(node => node.ShortestLine)
            .ToHashSet()
            .ToList();

        DebugLineLengths(nodes.Values.ToList());
        DebugLines(uniqueShortestLines);

        // Determine if choke

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

    private static List<Line> CreateLines(int angleInDegrees) {
        var lineLength = (int)Math.Ceiling(Math.Sqrt(MaxX * MaxX + MaxY * MaxY));
        var origin = new Vector3 { X = MaxX / 2f, Y = MaxY / 2f };
        var paddingX = (int)(lineLength / 2f - origin.X);
        var paddingY = (int)(lineLength / 2f - origin.Y);
        var angleInRadians = DegToRad(angleInDegrees);

        var lines = new List<Line>();
        for (var y = -paddingY; y < MaxY + paddingY; y++) {
            var start = new Vector3(-paddingX, y, 0).Rotate2D(angleInRadians, origin);
            var end = new Vector3(MaxX + paddingX, y, 0).Rotate2D(angleInRadians, origin);

            lines.Add(new Line(start, end, angleInDegrees));
        }

        return lines;
    }

    private static double DegToRad(double degrees) {
        return Math.PI / 180 * degrees;
    }

    private static List<Line> BreakDown(List<Line> lines) {
        return lines.SelectMany(BreakDown).ToList();
    }

    private static List<Line> BreakDown(Line line) {
        var lines = new List<Line>();

        var cellIndex = 0;
        while (cellIndex < line.OrderedTraversedCells.Count) {
            var startCellIndex = GoToStartOfNextLine(line, cellIndex);
            if (startCellIndex < 0) {
                break;
            }

            var endCellIndex = GoToEndOfLine(line, startCellIndex);

            var cells = line.OrderedTraversedCells
                .Skip(startCellIndex)
                .Take(endCellIndex - startCellIndex + 1)
                .ToList();

            lines.Add(new Line(cells, line.Angle));

            cellIndex = endCellIndex + 1;
        }

        return lines;
    }

    /// <summary>
    /// Iterate over the line's cells until we meet a walkable cell.
    /// Returns the index of that cell, or -1 if not found.
    /// </summary>
    /// <param name="line">The line to iterate over</param>
    /// <param name="startCellIndex">The cell index to start at</param>
    /// <returns>The index of the next walkable cell or -1 if none</returns>
    private static int GoToStartOfNextLine(Line line, int startCellIndex) {
        var currentCellIndex = startCellIndex;
        while (currentCellIndex < line.OrderedTraversedCells.Count) {
            if (MapAnalyzer.IsWalkable(line.OrderedTraversedCells[currentCellIndex], includeObstacles: false)) {
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
    /// <param name="line">The line to iterate over</param>
    /// <param name="startCellIndex">The cell index to start at</param>
    /// <returns>The index of the last walkable cell or -1 if none</returns>
    private static int GoToEndOfLine(Line line, int startCellIndex) {
        if (startCellIndex < 0) {
            return startCellIndex;
        }

        var currentCellIndex = startCellIndex;

        while (currentCellIndex < line.OrderedTraversedCells.Count) {
            if (!MapAnalyzer.IsWalkable(line.OrderedTraversedCells[currentCellIndex], includeObstacles: false)) {
                return currentCellIndex - 1;
            }

            currentCellIndex++;
        }

        return currentCellIndex - 1;
    }

    private static void DebugLineLengths(List<Node> nodes) {
        var minLength = nodes.Min(node => node.ShortestLine.Length);
        var maxLength = nodes.Max(node => node.ShortestLine.Length);

        foreach (var node in nodes) {
            var textColor = Colors.Gradient(Colors.DarkRed, Colors.White, LinScale(node.ShortestLine.Length, minLength, maxLength));
            Program.GraphicalDebugger.AddText($"{node.ShortestLine.Length:F0}", worldPos: node.Position.WithWorldHeight().ToPoint(), color: textColor, size: 13);
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

    private static void DebugLines(List<Line> lines) {
        foreach (var line in lines) {
            Program.GraphicalDebugger.AddLink(line.Start.WithWorldHeight(), line.End.WithWorldHeight(), Colors.Orange, withText: false);
        }
    }
}
