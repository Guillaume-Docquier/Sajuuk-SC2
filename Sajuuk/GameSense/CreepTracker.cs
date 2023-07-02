using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sajuuk.ExtensionMethods;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.GameData;
using SC2APIProtocol;

namespace Sajuuk.GameSense;

public class CreepTracker : ICreepTracker, INeedUpdating {
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IFrameClock _frameClock;
    private readonly IGraphicalDebugger _graphicalDebugger;

    private ulong _creepMapLastGeneratedAt = ulong.MaxValue;
    private List<List<bool>> _creepMap;

    private ulong _creepFrontierLastGeneratedAt = ulong.MaxValue;
    private List<Vector2> _creepFrontier = new List<Vector2>();

    private ImageData _rawCreepMap;

    private int _maxX;
    private int _maxY;

    public CreepTracker(
        IVisibilityTracker visibilityTracker,
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IFrameClock frameClock,
        IGraphicalDebugger graphicalDebugger
    ) {
        _visibilityTracker = visibilityTracker;
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _frameClock = frameClock;
        _graphicalDebugger = graphicalDebugger;
    }

    public void Reset() {}

    public void Update(ResponseObservation observation, ResponseGameInfo gameInfo) {
        _maxX = gameInfo.StartRaw.MapSize.X;
        _maxY = gameInfo.StartRaw.MapSize.Y;

        _rawCreepMap = observation.Observation.RawData.MapState.Creep;

        _creepFrontier.ForEach(creepFrontierNode => _graphicalDebugger.AddGridSquare(_terrainTracker.WithWorldHeight(creepFrontierNode), Colors.Orange));
    }

    public bool HasCreep(Vector2 position) {
        if (!_terrainTracker.IsInBounds(position)) {
            Logger.Error("HasCreep called on out of bounds position");
            return false;
        }

        if (_creepMapLastGeneratedAt != _frameClock.CurrentFrame) {
            GenerateCreepMap();
        }

        return _creepMap[(int)position.X][(int)position.Y];
    }

    public List<Vector2> GetCreepFrontier() {
        if (_creepFrontierLastGeneratedAt != _frameClock.CurrentFrame) {
            GenerateCreepFrontier();
        }

        return _creepFrontier;
    }

    private void GenerateCreepFrontier() {
        // TODO GD At this point, we don't need to calculate the frontier until a hatch or creep tumor dies
        if (_terrainTracker.WalkableCells.All(HasCreep)) {
            _creepFrontier = new List<Vector2>();
            _creepFrontierLastGeneratedAt = _frameClock.CurrentFrame;

            return;
        }

        var creepTumors = _unitsTracker.GetUnits(_unitsTracker.OwnedUnits, Units.CreepTumor).ToList();
        _creepFrontier = _terrainTracker.WalkableCells
            .Where(_visibilityTracker.IsVisible)
            .Where(HasCreep)
            .Where(IsFrontier)
            .Where(frontierCell => !IsTooCrowded(frontierCell, creepTumors))
            .ToList();

        _creepFrontierLastGeneratedAt = _frameClock.CurrentFrame;
    }

    private bool IsFrontier(Vector2 position) {
        return position.GetNeighbors()
            .Where(neighbor => _terrainTracker.IsWalkable(neighbor))
            // We spread towards non visible creep because if it is not visible, it is receding (creep source died) or it is not our creep and we want the vision
            .Any(neighbor => !HasCreep(neighbor) || !_visibilityTracker.IsVisible(neighbor));
    }

    private static bool IsTooCrowded(Vector2 frontierCell, IReadOnlyCollection<Unit> creepTumors) {
        if (creepTumors.Count <= 0) {
            return false;
        }

        // Prevent clumps around fresh tumors. Creep will spread soon.
        return creepTumors.Min(tumor => tumor.DistanceTo(frontierCell)) < 7;
    }

    private void GenerateCreepMap() {
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

        _creepMapLastGeneratedAt = _frameClock.CurrentFrame;
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
