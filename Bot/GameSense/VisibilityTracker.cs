using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using SC2APIProtocol;

namespace Bot.GameSense;

public class VisibilityTracker : IVisibilityTracker, INeedUpdating {
    /// <summary>
    /// DI: ✔️ The only usages are for static instance creations
    /// </summary>
    public static VisibilityTracker Instance { get; private set; } = new VisibilityTracker();

    // TODO GD Put these in a class to hide the backing fields?
    private ulong _lastGeneratedAt = ulong.MaxValue;

    private List<List<Visibility>> _visibilityMap;
    private List<List<Visibility>> VisibilityMap {
        get {
            if (_lastGeneratedAt != Controller.Frame) {
                GenerateVisibilityData();
            }

            return _visibilityMap;
        }
    }

    private List<Vector2> _visibleCells;
    public List<Vector2> VisibleCells {
        get {
            if (_lastGeneratedAt != Controller.Frame) {
                GenerateVisibilityData();
            }

            return _visibleCells;
        }
    }

    private List<Vector2> _exploredCells;
    public List<Vector2> ExploredCells {
        get {
            if (_lastGeneratedAt != Controller.Frame) {
                GenerateVisibilityData();
            }

            return _exploredCells;
        }
    }

    private ImageData _rawVisibilityMap;

    private enum Visibility {
        NotExplored = 0,
        Explored = 1,
        Visible = 2,
    }
    private VisibilityTracker() {}

    public void Reset() {
        Instance = new VisibilityTracker();
    }

    public void Update(ResponseObservation observation, ResponseGameInfo gameInfo) {
        _rawVisibilityMap = observation.Observation.RawData.MapState.Visibility;
    }

    // TODO GD We could be smarter and check for the building footprint!
    public bool IsVisible(Unit unit) {
        return IsVisible(unit.Position.ToVector2());
    }

    public bool IsVisible(Vector3 location) {
        return IsVisible(location.ToVector2());
    }

    public bool IsVisible(Vector2 location) {
        return VisibilityMap[(int)location.X][(int)location.Y] is Visibility.Visible;
    }

    // TODO GD We could be smarter and check for the building footprint!
    public bool IsExplored(Unit unit) {
        return IsExplored(unit.Position.ToVector2());
    }

    public bool IsExplored(Vector3 location) {
        return IsExplored(location.ToVector2());
    }

    public bool IsExplored(Vector2 location) {
        return VisibilityMap[(int)location.X][(int)location.Y] is Visibility.Explored or Visibility.Visible;
    }

    private void GenerateVisibilityData() {
        var maxX = Controller.GameInfo.StartRaw.MapSize.X;
        var maxY = Controller.GameInfo.StartRaw.MapSize.Y;

        _visibilityMap = new List<List<Visibility>>();
        _visibleCells = new List<Vector2>();
        _exploredCells = new List<Vector2>();
        for (var x = 0; x < maxX; x++) {
            _visibilityMap.Add(new List<Visibility>(new Visibility[maxY]));
        }

        var visibilityVector = _rawVisibilityMap.Data
            .ToByteArray()
            .ToList();

        for (var x = 0; x < maxX; x++) {
            for (var y = 0; y < maxY; y++) {
                var visibility = (Visibility)visibilityVector[y * maxX + x]; // visibilityVector[4] is (4, 0);
                _visibilityMap[x][y] = visibility;

                if (visibility == Visibility.Visible) {
                    _visibleCells.Add(new Vector2(x, y).AsWorldGridCenter());
                    _exploredCells.Add(new Vector2(x, y).AsWorldGridCenter());
                }
                else if (visibility == Visibility.Explored) {
                    _exploredCells.Add(new Vector2(x, y).AsWorldGridCenter());
                }
            }
        }

        _lastGeneratedAt = Controller.Frame;
    }
}
