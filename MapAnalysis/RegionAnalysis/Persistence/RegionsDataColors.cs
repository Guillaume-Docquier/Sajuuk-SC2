using System.Drawing;
using SC2Client.Debugging.GraphicalDebugging;

namespace MapAnalysis.RegionAnalysis.Persistence;

public static class RegionsDataColors {
    public static readonly Color MineralColor = Color.Cyan;
    public static readonly Color GasColor = Color.Lime;
    public static readonly Color ExpandColor = Color.Magenta;

    // The colors match those used in AnalyzedRegion
    public static readonly Dictionary<SC2APIProtocol.Color, Color> RegionColorsMapping = new Dictionary<SC2APIProtocol.Color, Color>
    {
        { Colors.Cyan, Color.Teal},
        { Colors.Magenta, Color.Purple},
        { Colors.Orange, Color.Olive},
        { Colors.Blue, Color.MediumBlue},
        { Colors.Red, Color.Maroon},
        { Colors.LimeGreen, Color.Green},
    };
}
