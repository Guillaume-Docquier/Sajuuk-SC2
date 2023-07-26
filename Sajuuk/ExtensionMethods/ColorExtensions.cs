using System;
using System.Drawing;

namespace Sajuuk.ExtensionMethods;

public static class ColorExtensions {
    public static Color Gradient(this Color start, Color end, float percent) {
        var deltaA = end.A - start.A;
        var deltaR = end.R - start.R;
        var deltaG = end.G - start.G;
        var deltaB = end.B - start.B;

        var gradient = Color.FromArgb(
            (int)Math.Round(start.A + deltaA * percent),
            (int)Math.Round(start.R + deltaR * percent),
            (int)Math.Round(start.G + deltaG * percent),
            (int)Math.Round(start.B + deltaB * percent)
        );

        return gradient;
    }
}
