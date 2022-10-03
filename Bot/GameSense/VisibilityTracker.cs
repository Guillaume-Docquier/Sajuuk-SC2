using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SC2APIProtocol;

namespace Bot.GameSense;

public class VisibilityTracker: INeedUpdating {
    public static readonly VisibilityTracker Instance = new VisibilityTracker();

    private static ulong _lastGeneratedAt = ulong.MaxValue;
    private static List<List<Visibility>> _visibilityMap;

    private static ImageData _rawVisibilityMap;

    private enum Visibility {
        NotExplored = 0,
        Explored = 1,
        Visible = 2,
    }
    private VisibilityTracker() {}

    public void Reset() {
        _lastGeneratedAt = ulong.MaxValue;
        _visibilityMap = null;
        _rawVisibilityMap = null;
    }

    public void Update(ResponseObservation observation) {
        _rawVisibilityMap = observation.Observation.RawData.MapState.Visibility;
    }

    public static bool IsVisible(Vector3 location) {
        if (_lastGeneratedAt != Controller.Frame) {
            GenerateVisibilityMap();
        }

        return _visibilityMap[(int)location.X][(int)location.Y] == Visibility.Visible;
    }

    private static void GenerateVisibilityMap() {
        var maxX = Controller.GameInfo.StartRaw.MapSize.X;
        var maxY = Controller.GameInfo.StartRaw.MapSize.Y;

        _visibilityMap = new List<List<Visibility>>();
        for (var x = 0; x < maxX; x++) {
            _visibilityMap.Add(new List<Visibility>(new Visibility[maxY]));
        }

        var visibilityVector = _rawVisibilityMap.Data
            .ToByteArray()
            .ToList();

        for (var x = 0; x < maxX; x++) {
            for (var y = 0; y < maxY; y++) {
                _visibilityMap[x][y] = (Visibility)visibilityVector[y * maxX + x]; // visibilityVector[4] is (4, 0)
            }
        }

        _lastGeneratedAt = Controller.Frame;
    }
}
