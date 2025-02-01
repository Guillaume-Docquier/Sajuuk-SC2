using System.Numerics;

namespace MapAnalysis.RegionAnalysis.Ramps;

public class Ramp {
    public HashSet<Vector2> Cells;

    public Ramp(HashSet<Vector2> cells) {
        Cells = cells;
    }
}
