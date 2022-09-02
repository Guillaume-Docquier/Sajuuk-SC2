using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.Wrapper;

/// <summary>
/// Implements all sorts of graphical shapes to help in local debugging.
/// </summary>
public class LocalGraphicalDebugger: IGraphicalDebugger {
    private const float CreepHeight = 0.02f;
    private const float Padding = 0.05f;

    private readonly List<DebugText> _debugTexts = new List<DebugText>();
    private readonly List<DebugSphere> _debugSpheres = new List<DebugSphere>();
    private readonly List<DebugBox> _debugBoxes = new List<DebugBox>();
    private readonly List<DebugLine> _debugLines = new List<DebugLine>();

    public Request GetDebugRequest() {
        var debugRequest = RequestBuilder.RequestDebug(_debugTexts, _debugSpheres, _debugBoxes, _debugLines);

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
        foreach (var cell in MapAnalyzer.BuildSearchRadius(centerPosition, radius)) {
            AddSquare(cell.WithWorldHeight(), KnowledgeBase.GameGridCellWidth, color, padded: true);
        }
    }

    public void AddSquare(Vector3 centerPosition, float width, Color color, bool padded = false) {
        AddRectangle(centerPosition, width, width, color, padded);
    }

    public void AddRectangle(Vector3 centerPosition, float width, float length, Color color, bool padded = false) {
        var padding = padded ? Padding : 0;

        _debugBoxes.Add(
            new DebugBox
            {
                Min = centerPosition.ToPoint(xOffset: -width / 2 + padding, yOffset: -length / 2 + padding, zOffset: CreepHeight),
                Max = centerPosition.ToPoint(xOffset:  width / 2 - padding, yOffset:  length / 2 - padding, zOffset: CreepHeight),
                Color = color,
            }
        );
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
        AddText("from", worldPos: start.ToPoint(), color: color);
        AddSphere(start, 1, color);

        AddLine(start, end, color);

        AddText("to", worldPos: end.ToPoint(), color: color);
        AddSphere(end, 1, color);
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
}
