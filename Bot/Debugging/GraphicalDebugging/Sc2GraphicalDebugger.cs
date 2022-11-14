using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.MapKnowledge;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.Debugging.GraphicalDebugging;

/// <summary>
/// Implements all sorts of graphical shapes to help in local debugging.
/// </summary>
public class Sc2GraphicalDebugger: IGraphicalDebugger {
    private const float CreepHeight = 0.02f;
    private const float Padding = 0.05f;

    private readonly List<DebugText> _debugTexts = new List<DebugText>();
    private readonly List<DebugSphere> _debugSpheres = new List<DebugSphere>();
    private readonly Dictionary<Vector3, List<DebugBox>> _debugBoxes = new Dictionary<Vector3, List<DebugBox>>();
    private readonly List<DebugLine> _debugLines = new List<DebugLine>();

    public Request GetDebugRequest() {
        var debugRequest = RequestBuilder.DebugDraw(
            _debugTexts,
            _debugSpheres,
            _debugBoxes.SelectMany(kv => kv.Value),
            _debugLines
        );

        _debugTexts.Clear();
        _debugSpheres.Clear();
        _debugBoxes.Clear();
        _debugLines.Clear();

        return debugRequest;
    }

    // Size doesn't work when pos is not defined
    public void AddText(string text, uint size = 15, Point virtualPos = null, Point worldPos = null, Color color = null) {
        _debugTexts.Add(new DebugText
        {
            Text = text,
            Size = size,
            VirtualPos = virtualPos,
            WorldPos = worldPos,
            Color = color ?? Colors.Yellow,
        });
    }

    public void AddTextGroup(IEnumerable<string> texts, uint size = 15, Point virtualPos = null, Point worldPos = null, Color color = null) {
        AddText(string.Join("\n", texts), size, virtualPos, worldPos, color);
    }

    public void AddGridSphere(Vector3 centerPosition, Color color = null) {
        AddSphere(centerPosition, KnowledgeBase.GameGridCellWidth / 2 - Padding, color ?? Colors.White);
    }

    public void AddSphere(Unit unit, Color color) {
        AddSphere(
            unit.Position,
            unit.Radius * 1.25f,
            color
        );
    }

    public void AddSphere(Vector3 position, float radius, Color color) {
        _debugSpheres.Add(
            new DebugSphere
            {
                P = position.ToPoint(zOffset: CreepHeight),
                R = radius,
                Color = color,
            }
        );
    }

    public void AddGridSquare(Vector3 centerPosition, Color color = null) {
        AddSquare(centerPosition, KnowledgeBase.GameGridCellWidth, color ?? Colors.White, padded: true);
    }

    public void AddGridSquaresInRadius(Vector3 centerPosition, int radius, Color color) {
        foreach (var cell in MapAnalyzer.BuildSearchRadius(centerPosition.ToVector2(), radius)) {
            AddSquare(cell.ToVector3(), KnowledgeBase.GameGridCellWidth, color, padded: true);
        }
    }

    public void AddSquare(Vector3 centerPosition, float width, Color color, bool padded = false) {
        AddRectangle(centerPosition, width, width, color, padded);
    }

    public void AddRectangle(Vector3 centerPosition, float width, float length, Color color, bool padded = false) {
        var padding = padded ? Padding : 0;
        var debugBox = new DebugBox
        {
            Min = centerPosition.ToPoint(xOffset: Math.Min(0, -width / 2 + padding), yOffset: Math.Min(0, -length / 2 + padding), zOffset: CreepHeight),
            Max = centerPosition.ToPoint(xOffset: Math.Max(0, width / 2 - padding), yOffset: Math.Max(0, length / 2 - padding), zOffset: CreepHeight),
            Color = color,
        };

        if (_debugBoxes.TryGetValue(centerPosition, out var storedDebugBoxes)) {
            if (!storedDebugBoxes.Any(storedDebugBox => storedDebugBox.Min.Equals(debugBox.Min) || storedDebugBox.Max.Equals(debugBox.Max))) {
                storedDebugBoxes.Add(debugBox);
            }
        }
        else {
            _debugBoxes.Add(centerPosition, new List<DebugBox> { debugBox });
        }
    }

    public void AddLine(Vector3 start, Vector3 end, Color color) {
        _debugLines.Add(
            new DebugLine
            {
                Line = new Line
                {
                    P0 = start.ToPoint(zOffset: CreepHeight),
                    P1 = end.ToPoint(zOffset: CreepHeight),
                },
                Color = color,
            }
        );
    }

    public void AddLink(Unit start, Unit end, Color color) {
        AddLink(start.Position, end.Position, color);
    }

    public void AddLink(Vector3 start, Vector3 end, Color color) {
        AddGridSphere(start, color);
        AddLine(start, end, color);
        AddGridSphere(end, color);
    }

    public void AddPath(List<Vector2> path, Color startColor, Color endColor) {
        AddPath(path.Select(cell => cell.ToVector3()).ToList(), startColor, endColor);
    }

    public void AddPath(List<Vector3> path, Color startColor, Color endColor) {
        if (path.Count <= 0) {
            return;
        }

        AddSphere(path[0], 1.5f, startColor);
        AddSphere(path[^1], 1.5f, endColor);
        for (var i = 0; i < path.Count; i++) {
            AddGridSquare(path[i], Colors.Gradient(startColor, endColor, (float)i / path.Count));
        }
    }

    public void AddUnit(Unit unit, Color color = null) {
        Program.GraphicalDebugger.AddSphere(unit.Position, unit.Radius * 1.25f, color ?? Colors.White);
        Program.GraphicalDebugger.AddText(
            $"{unit.UnitTypeData.Name}",
            size: 13,
            worldPos: unit.Position.Translate(zTranslation: unit.Radius * 1.25f).ToPoint()
        );

        if (unit.IsFlying) {
            var groundPosition = unit.Position.WithWorldHeight();
            Program.GraphicalDebugger.AddLine(unit.Position, groundPosition, color ?? Colors.White);
            Program.GraphicalDebugger.AddGridSphere(groundPosition, color ?? Colors.White);
        }
    }
}
