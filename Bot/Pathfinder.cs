using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Bot;

public static class Pathfinder {
    private static List<List<float>> _heightMap;
    private static List<List<bool>> _walkMap;

    public static bool IsInitialized = false;

    public static void Init() {
        if (IsInitialized) {
            return;
        }

        InitHeightMap();
        InitWalkMap();

        IsInitialized = true;
    }

    private static void InitHeightMap() {
        var maxX = Controller.GameInfo.StartRaw.MapSize.X;
        var maxY = Controller.GameInfo.StartRaw.MapSize.Y;

        _heightMap = new List<List<float>>();
        for (var x = 0; x < maxX; x++) {
            _heightMap.Add(new List<float>(new float[maxY]));
        }

        var heightVector = Controller.GameInfo.StartRaw.TerrainHeight.Data
            .ToByteArray()
            .Select(ByteToFloat)
            .ToList();

        for (var x = 0; x < maxX; x++) {
            for (var y = 0; y < maxY; y++) {
                _heightMap[x][y] = heightVector[y * maxX + x]; // heightVector[4] is (4, 0)
            }
        }
    }

    private static void InitWalkMap() {
        var maxX = Controller.GameInfo.StartRaw.MapSize.X;
        var maxY = Controller.GameInfo.StartRaw.MapSize.Y;

        _walkMap = new List<List<bool>>();
        for (var x = 0; x < maxX; x++) {
            _walkMap.Add(new List<bool>(new bool[maxY]));
        }

        var walkVector = Controller.GameInfo.StartRaw.PathingGrid.Data
            .ToByteArray()
            .SelectMany(ByteToBoolArray)
            .ToList();

        for (var x = 0; x < maxX; x++) {
            for (var y = 0; y < maxY; y++) {
                _walkMap[x][y] = walkVector[y * maxX + x]; // walkVector[4] is (4, 0)
            }
        }
    }

    private static List<Vector3> AStar(Vector3 origin, Vector3 destination) {
        return default;
    }

    private static float ByteToFloat(byte byteValue) {
        // Computed from 3 unit positions and 3 height map bytes
        // Seems to work fine
        return 0.125f * byteValue - 15.888f;
    }

    private static bool[] ByteToBoolArray(byte byteValue)
    {
        // Each byte represents 8 grid cells
        var values = new bool[8];

        values[7] = (byteValue & 1) != 0;
        values[6] = (byteValue & 2) != 0;
        values[5] = (byteValue & 4) != 0;
        values[4] = (byteValue & 8) != 0;
        values[3] = (byteValue & 16) != 0;
        values[2] = (byteValue & 32) != 0;
        values[1] = (byteValue & 64) != 0;
        values[0] = (byteValue & 128) != 0;

        return values;
    }
}
