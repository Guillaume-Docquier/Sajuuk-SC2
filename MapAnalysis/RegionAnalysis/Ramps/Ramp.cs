using System.Numerics;
using System.Text.Json.Serialization;

namespace MapAnalysis.RegionAnalysis.Ramps;

public class Ramp {
    public HashSet<Vector2> Cells;

    [JsonConstructor]
    [Obsolete("Do not use this parameterless JsonConstructor", error: true)]
#pragma warning disable CS8618, CS9264
    public Ramp() {}
#pragma warning restore CS8618, CS9264

    public Ramp(HashSet<Vector2> cells) {
        Cells = cells;
    }
}
