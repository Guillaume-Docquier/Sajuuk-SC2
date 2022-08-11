using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.GameSense;

public class CreepTracker: INeedUpdating {
    public static readonly CreepTracker Instance = new CreepTracker();

    private static ulong _creepMapLastGeneratedAt = ulong.MaxValue;
    private static List<List<bool>> _creepMap;

    private static ulong _creepFrontierLastGeneratedAt = ulong.MaxValue;
    private static List<Vector3> _creepFrontier;

    private static ImageData _rawCreepMap;

    private static int _maxX;
    private static int _maxY;

    private CreepTracker() {}

    public void Update(ResponseObservation observation) {
        _maxX = Controller.GameInfo.StartRaw.MapSize.X;
        _maxY = Controller.GameInfo.StartRaw.MapSize.Y;

        _rawCreepMap = observation.Observation.RawData.MapState.Creep;
    }

    public static bool HasCreep(Vector3 position) {
        if (!MapAnalyzer.IsInBounds(position)) {
            Logger.Error("HasCreep called on out of bounds position");
            return false;
        }

        if (_creepMapLastGeneratedAt != Controller.Frame) {
            GenerateCreepMap();
        }

        return _creepMap[(int)position.X][(int)position.Y];
    }

    public static IEnumerable<Vector3> GetCreepFrontier() {
        if (_creepFrontierLastGeneratedAt != Controller.Frame) {
            GenerateCreepFrontier();
        }

        return _creepFrontier;
    }

    private static void GenerateCreepFrontier() {
        _creepFrontier = new List<Vector3>();

        var creepTumors = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.CreepTumor).ToList();
        for (var x = 0; x < _maxX; x++) {
            for (var y = 0; y < _maxY; y++) {
                var position = new Vector3(x, y, 0).AsWorldGridCenter().WithWorldHeight();
                // We spread towards non visible creep because if it is not visible, it is receding (tumor died) or it is not our creep and we want the vision
                if (HasCreep(position) && (TouchesNonCreep(position) || TouchesNonVisibleCreep(position))) {
                    // On GlitteringAshes there is a spot that is walkable but cannot have creep
                    // If we have a tumor close to it, consider that it has creep
                    if (creepTumors.Count > 0 && creepTumors.Min(tumor => tumor.HorizontalDistanceTo(position)) < 1.5) {
                        continue;
                    }

                    _creepFrontier.Add(position);
                }
            }
        }

        _creepFrontierLastGeneratedAt = Controller.Frame;
    }

    private static bool TouchesNonCreep(Vector3 position) {
        return position.GetNeighbors().Any(neighbor => !HasCreep(neighbor) && MapAnalyzer.IsWalkable(neighbor));
    }

    private static bool TouchesNonVisibleCreep(Vector3 position) {
        return position.GetNeighbors().Any(neighbor => HasCreep(neighbor) && MapAnalyzer.IsWalkable(neighbor) && !VisibilityTracker.IsVisible(neighbor));
    }

    private static void GenerateCreepMap() {
        _creepMap = new List<List<bool>>();
        for (var x = 0; x < _maxX; x++) {
            _creepMap.Add(new List<bool>(new bool[_maxY]));
        }

        var creepVector = _rawCreepMap.Data
            .ToByteArray()
            .SelectMany(ByteToBoolArray)
            .ToList();

        for (var x = 0; x < _maxX; x++) {
            for (var y = 0; y < _maxY; y++) {
                _creepMap[x][y] = creepVector[y * _maxX + x]; // creepVector[4] is (4, 0)
            }
        }

        _creepMapLastGeneratedAt = Controller.Frame;
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
