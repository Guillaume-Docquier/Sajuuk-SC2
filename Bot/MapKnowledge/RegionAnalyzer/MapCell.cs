using System.Numerics;

namespace Bot.MapKnowledge;

public class MapCell : IHavePosition {
    public MapCell(Vector3 position) {
        Position = position;
    }

    public Vector3 Position { get; set; }
}
