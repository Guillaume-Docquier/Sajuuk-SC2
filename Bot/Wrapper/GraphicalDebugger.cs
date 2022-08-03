using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.Wrapper;

public static class GraphicalDebugger {
    private const float CreepHeight = 0.01f;
    private const float Padding = 0.05f;

    private static readonly List<DebugText> DebugTexts = new List<DebugText>();
    private static readonly List<DebugSphere> DebugSpheres = new List<DebugSphere>();
    private static readonly List<DebugBox> DebugBoxes = new List<DebugBox>();
    private static readonly List<DebugLine> DebugLines = new List<DebugLine>();

    // TODO GD Add some optional persistence to the debug elements otherwise they last only 1 frame
    public static Request GetDebugRequest() {
        var request = RequestBuilder.DebugRequest(DebugTexts, DebugSpheres, DebugBoxes, DebugLines);

        DebugTexts.Clear();
        DebugSpheres.Clear();
        DebugBoxes.Clear();
        DebugLines.Clear();

        return request;
    }

    // Size doesn't work when pos is not defined
    public static void AddText(string text, uint size = 15, Point virtualPos = null, Point worldPos = null, Color color = null) {
        DebugTexts.Add(new DebugText
        {
            Text = text,
            Size = size,
            VirtualPos = virtualPos,
            WorldPos = worldPos,
            Color = color ?? Colors.Yellow,
        });
    }

    public static void AddTextGroup(IEnumerable<string> texts, uint size = 15, Point virtualPos = null, Point worldPos = null, Color color = null) {
        AddText(string.Join("\n", texts), size, virtualPos, worldPos, color);
    }

    public static void AddSphere(Unit unit, Color color) {
        AddSphere(
            unit.Position,
            unit.Radius * 1.25f,
            color
        );
    }

    public static void AddSphere(Vector3 position, float radius, Color color) {
        DebugSpheres.Add(
            new DebugSphere
            {
                P = position.ToPoint(zOffset: CreepHeight),
                R = radius,
                Color = color,
            }
        );
    }

    public static void AddGridSquare(Vector3 centerPosition, Color color) {
        AddSquare(centerPosition, KnowledgeBase.GameGridCellWidth, color, padded: true);
    }

    public static void AddGridSquaresInRadius(Vector3 centerPosition, float radius, Color color) {
        var cells = MapAnalyzer.BuildSearchGrid(centerPosition, radius).Where(cell => cell.HorizontalDistanceTo(centerPosition) <= radius);

        foreach (var cell in cells) {
            AddSquare(cell.WithWorldHeight(), KnowledgeBase.GameGridCellWidth, color, padded: true);
        }
    }

    public static void AddSquare(Vector3 centerPosition, float width, Color color, bool padded = false) {
        AddRectangle(centerPosition, width, width, color, padded);
    }

    public static void AddRectangle(Vector3 centerPosition, float width, float length, Color color, bool padded = false) {
        var padding = padded ? Padding : 0;

        DebugBoxes.Add(
            new DebugBox
            {
                Min = new Point { X = centerPosition.X - width / 2 + padding, Y = centerPosition.Y - length / 2 + padding, Z = centerPosition.Z + CreepHeight },
                Max = new Point { X = centerPosition.X + width / 2 - padding, Y = centerPosition.Y + length / 2 - padding, Z = centerPosition.Z + CreepHeight },
                Color = color,
            }
        );
    }

    public static void AddLine(Vector3 start, Vector3 end, Color color) {
        DebugLines.Add(
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

    public static void AddLink(Unit start, Unit end, Color color) {
        AddLink(start.Position, end.Position, color);
    }

    public static void AddLink(Vector3 start, Vector3 end, Color color) {
        AddText("from", worldPos: start.ToPoint(), color: color);
        AddSphere(start, 1, color);

        AddLine(start, end, color);

        AddText("to", worldPos: end.ToPoint(), color: color);
        AddSphere(end, 1, color);
    }
}

public static class Colors {
    public static Color Gradient(Color start, Color end, float percent) {
        var deltaR = (int)end.R - (int)start.R;
        var deltaG = (int)end.G - (int)start.G;
        var deltaB = (int)end.B - (int)start.B;

        var gradient = new Color
        {
            R = (uint)Math.Round(start.R + deltaR * percent),
            G = (uint)Math.Round(start.G + deltaG * percent),
            B = (uint)Math.Round(start.B + deltaB * percent),
        };

        return gradient;
    }

    public static readonly Color White = new Color { R = 1, G = 1, B = 1 };
    public static readonly Color Black = new Color { R = 255, G = 255, B = 255 };

    public static readonly Color Red = new Color { R = 255, G = 1, B = 1 };
    public static readonly Color Green = new Color { R = 1, G = 255, B = 1 };
    public static readonly Color Blue = new Color { R = 1, G = 1, B = 255 };

    public static readonly Color Yellow = new Color { R = 255, G = 255, B = 1 };
    public static readonly Color Cyan = new Color { R = 1, G = 255, B = 255 };
    public static readonly Color Magenta = new Color { R = 255, G = 1, B = 255 };

    public static readonly Color DarkGreen = new Color { R = 1, G = 100, B = 1 };
    public static readonly Color DarkBlue = new Color { R = 1, G = 1, B = 139 };
    public static readonly Color Maroon3 = new Color { R = 176, G = 48, B = 96 };
    public static readonly Color Burlywood = new Color { R = 222, G = 184, B = 135 };
    public static readonly Color Cornflower = new Color { R = 100, G = 149, B = 237 };
    public static readonly Color Lime = new Color { R = 175, G = 255, B = 1 };
    public static readonly Color Orange = new Color { R = 226, G = 131, B = 36 };
    public static readonly Color Purple = new Color { R = 153, G = 51, B = 255 };
}
