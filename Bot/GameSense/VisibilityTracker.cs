﻿using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using SC2APIProtocol;

namespace Bot.GameSense;

public class VisibilityTracker: INeedUpdating {
    public static readonly VisibilityTracker Instance = new VisibilityTracker();

    private static ulong _lastGeneratedAt = ulong.MaxValue;
    private static List<List<Visibility>> _visibilityMap;
    private static List<Vector2> _visibleCells;
    private static List<Vector2> _exploredCells;

    public static List<Vector2> VisibleCells {
        get {
            if (_lastGeneratedAt != Controller.Frame) {
                GenerateVisibilityMap();
            }

            return _visibleCells;
        }
    }

    public static List<Vector2> ExploredCells {
        get {
            if (_lastGeneratedAt != Controller.Frame) {
                GenerateVisibilityMap();
            }

            return _exploredCells;
        }
    }

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
        _visibleCells = null;
        _exploredCells = null;
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
