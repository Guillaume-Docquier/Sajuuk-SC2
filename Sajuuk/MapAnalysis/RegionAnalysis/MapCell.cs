using System.Numerics;

namespace Sajuuk.MapAnalysis.RegionAnalysis;

public class MapCell : IHavePosition {
    public MapCell(Vector3 position) {
        Position = position;
    }

    public Vector3 Position { get; set; }
}
