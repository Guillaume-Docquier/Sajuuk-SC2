using System;
using System.Linq;
using SC2APIProtocol;

namespace Bot.Wrapper;

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

    private static Color BrightColor(int r, int g, int b, float brightness = 1f) {
        var max = new [] { r, g, b }.Max();
        var brightnessMultiplier = 255f / max * brightness;

        return new Color { R = (uint)(r * brightnessMultiplier), G = (uint)(g * brightnessMultiplier), B = (uint)(b * brightnessMultiplier)};
    }

    public static readonly Color White = new Color { R = 255, G = 255, B = 255 };
    public static readonly Color Black = new Color { R = 1, G = 1, B = 1 };

    public static readonly Color Red = new Color { R = 255, G = 1, B = 1 };
    public static readonly Color Green = new Color { R = 1, G = 255, B = 1 };
    public static readonly Color Blue = new Color { R = 1, G = 1, B = 255 };

    public static readonly Color LightRed = new Color { R = 255, G = 100, B = 100 };
    public static readonly Color LightGreen = new Color { R = 144, G = 238, B = 144 };
    public static readonly Color LightBlue = new Color { R = 173, G = 216, B = 230 };

    public static readonly Color Yellow = new Color { R = 255, G = 255, B = 1 };
    public static readonly Color Cyan = new Color { R = 1, G = 255, B = 255 };
    public static readonly Color Magenta = new Color { R = 255, G = 1, B = 255 };

    public static readonly Color DarkRed = new Color { R = 175, G = 1, B = 1 };
    public static readonly Color DarkGreen = new Color { R = 1, G = 175, B = 1 };
    public static readonly Color DarkBlue = new Color { R = 1, G = 50, B = 200 };

    public static readonly Color MaroonRed = new Color { R = 176, G = 48, B = 96 };
    public static readonly Color BurlywoodBeige = new Color { R = 222, G = 184, B = 135 };
    public static readonly Color CornflowerBlue = new Color { R = 100, G = 149, B = 237 };
    public static readonly Color LimeGreen = new Color { R = 175, G = 255, B = 1 };
    public static readonly Color Orange = new Color { R = 226, G = 131, B = 36 };
    public static readonly Color Purple = new Color { R = 153, G = 51, B = 255 };

    public static readonly Color MulberryRed = BrightColor(r: 127, g: 59, b: 95, brightness: 0.75f);
    public static readonly Color MediumTurquoise = new Color { R = 72, G = 209, B = 204 };
    public static readonly Color SunbrightOrange = new Color { R = 253, G = 184, B = 19 };
    public static readonly Color PeachPink = new Color { R = 255, G = 209, B = 193 };
}
