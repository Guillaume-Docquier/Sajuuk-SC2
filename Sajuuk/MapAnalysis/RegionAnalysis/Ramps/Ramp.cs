using System.Collections.Generic;
using System.Numerics;

namespace Sajuuk.MapAnalysis.RegionAnalysis.Ramps;

public class Ramp {
    public HashSet<Vector2> Cells;

    public Ramp(HashSet<Vector2> cells) {
        Cells = cells;
    }
}
