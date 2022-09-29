using System.Numerics;
using Bot.ExtensionMethods;

namespace Bot.MapKnowledge;

public class MapCell: IHavePosition {
    public MapCell(Vector3 position) {
        Position = position;
    }

    public MapCell(float x, float y, bool withWorldHeight = true) {
        var position = new Vector3(x, y, 0).AsWorldGridCenter().WithWorldHeight();
        if (!withWorldHeight) {
            position.Z = 0;
        }

        Position = position;
    }

    public Vector3 Position { get; set; }
}
