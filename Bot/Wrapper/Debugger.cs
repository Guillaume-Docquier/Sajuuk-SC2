using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SC2APIProtocol;

namespace Bot.Wrapper;

public static class Debugger {
    private static readonly List<DebugText> _debugTexts = new List<DebugText>();
    private static readonly List<DebugSphere> _debugSpheres = new List<DebugSphere>();

    public static Request GetDebugRequest() {
        var request = new Request
        {
            Debug = new RequestDebug
            {
                Debug =
                {
                    new DebugCommand
                    {
                        Draw = new DebugDraw
                        {
                            Text = { _debugTexts },
                            Spheres = { _debugSpheres },
                        },
                    },
                }
            }
        };

        _debugTexts.Clear();
        _debugSpheres.Clear();

        return request;
    }

    public static void AddDebugText(string text) {
        _debugTexts.Add(new DebugText { Text = text, Size = 18 });
    }

    public static void AddDebugText(IEnumerable<string> texts) {
        _debugTexts.AddRange(texts.Select(text => new DebugText { Text = text, Size = 18 }));
    }

    public static void AddSphere(Unit unit, Color color) {
        _debugSpheres.Add(
            new DebugSphere
            {
                Color = color, // TODO GD Doesn't work, always white
                P = new Point { X = unit.Position.X, Y = unit.Position.Y, Z = unit.Position.Z },
                R = unit.Radius * 1.25f,
            }
        );
    }

    public static void AddSphere(Vector3 position, float radius, Color color) {
        _debugSpheres.Add(
            new DebugSphere
            {
                Color = color, // TODO GD Doesn't work, always white
                P = new Point { X = position.X, Y = position.Y },
                R = radius,
            }
        );
    }
}

public static class Colors {
    public static Color Red = new Color { R = 255, G = 0, B = 0 };
    public static Color Green = new Color { R = 0, G = 255, B = 0 };
    public static Color Blue = new Color { R = 0, G = 0, B = 255 };

    public static Color Yellow = new Color { R = 255, G = 255, B = 0 };
    public static Color Cyan = new Color { R = 0, G = 255, B = 255 };
    public static Color Magenta = new Color { R = 255, G = 0, B = 255 };
}
