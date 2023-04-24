using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using SC2APIProtocol;

namespace Bot.MapKnowledge;

// TODO GD Make two classes: The analyzer and the tracker
// Analysis should be run manually, and the tracker should be able to load the persisted data before entering the game
public class MapAnalyzer: INeedUpdating, IWatchUnitsDie {
    public static readonly MapAnalyzer Instance = new MapAnalyzer(VisibilityTracker.Instance, UnitsTracker.Instance);

    private readonly IVisibilityTracker _visibilityTracker;
    private readonly IUnitsTracker _unitsTracker;

    public static bool IsInitialized { get; private set; } = false;
    public static Vector2 StartingLocation { get; private set; }
    public static Vector2 EnemyStartingLocation { get; private set; }

    public static List<List<float>> HeightMap { get; private set; }

    private static List<Unit> _obstacles;
    private static readonly HashSet<Vector2> ObstructionMap = new HashSet<Vector2>();
    private static List<List<bool>> _terrainWalkMap;
    private static List<List<bool>> _currentWalkMap;
    private static List<List<bool>> _buildMap;

    public static int MaxX { get; private set; }
    public static int MaxY { get; private set; }
    public static float DiagonalLength { get; private set; }

    private static readonly HashSet<Vector2> _walkableCells = new HashSet<Vector2>();
    public static IReadOnlySet<Vector2> WalkableCells => _walkableCells;

    /// <summary>
    /// Returns the proportion from 0 to 1 of the walkable tiles that have been explored
    /// </summary>
    public float ExplorationRatio {
        get {
            if (_walkableCells.Count == 0) {
                return 0;
            }

            var exploredCellsCount = _visibilityTracker.ExploredCells.Count(cell => IsWalkable(cell));
            return (float)exploredCellsCount / _walkableCells.Count;
        }
    }

    /// <summary>
    /// Returns the proportion from 0 to 1 of the walkable tiles that are currently visible
    /// </summary>
    public float VisibilityRatio {
        get {
            if (_walkableCells.Count == 0) {
                return 0;
            }

            var visibleCellsCount = _visibilityTracker.VisibleCells.Count(cell => IsWalkable(cell));
            return (float)visibleCellsCount / _walkableCells.Count;
        }
    }

    private MapAnalyzer(IVisibilityTracker visibilityTracker, IUnitsTracker unitsTracker) {
        _visibilityTracker = visibilityTracker;
        _unitsTracker = unitsTracker;
    }

    public void Reset() {
        IsInitialized = false;
        StartingLocation = default;
        EnemyStartingLocation = default;

        HeightMap = null;

        _obstacles = null;
        ObstructionMap.Clear();
        _terrainWalkMap = null;
        _currentWalkMap = null;
        _walkableCells.Clear();
    }

    public void Update(ResponseObservation observation, ResponseGameInfo gameInfo) {
        if (IsInitialized) {
            _currentWalkMap = ParseWalkMap();
            return;
        }

        MaxX = Controller.GameInfo.StartRaw.MapSize.X;
        MaxY = Controller.GameInfo.StartRaw.MapSize.Y;
        DiagonalLength = (float)Math.Sqrt(MaxX * MaxX + MaxY * MaxY);

        _currentWalkMap = ParseWalkMap();

        InitSpawnLocations();
        InitObstacles();

        InitHeightMap();
        InitTerrainWalkMap();
        InitTerrainBuildMap();

        InitWalkableCells();

        IsInitialized = true;
    }

    public static string GetStartingCorner() {
        var corners = new List<(Vector2 Position, string Name)>
        {
            (new Vector2(0,    0),    "bottom left"),
            (new Vector2(MaxX, 0),    "bottom right"),
            (new Vector2(0,    MaxY), "top left"),
            (new Vector2(MaxX, MaxY), "top right"),
        };

        return corners.MinBy(corner => corner.Position.DistanceTo(StartingLocation)).Name;
    }

    public void ReportUnitDeath(Unit deadUnit) {
        RemoveObstacle(deadUnit);
    }

    private void InitObstacles() {
        var obstacleIds = new HashSet<uint>(Units.Obstacles.Concat(Units.MineralFields).Concat(Units.GasGeysers));
        obstacleIds.Remove(Units.UnbuildablePlatesDestructible); // It is destructible but you can walk on it

        _obstacles = Controller.GetUnits(_unitsTracker.NeutralUnits, obstacleIds).ToList();

        _obstacles.ForEach(obstacle => {
            obstacle.AddDeathWatcher(Instance);
            foreach (var cell in FootprintCalculator.GetFootprint(obstacle)) {
                ObstructionMap.Add(cell);
            }
        });
    }

    private static void RemoveObstacle(Unit obstacle) {
        _obstacles.Remove(obstacle);
        foreach (var cell in FootprintCalculator.GetFootprint(obstacle)) {
            ObstructionMap.Remove(cell);
        }

        Logger.Info("Obstacle removed, invalidating Pathfinder cache and updating region obstruction");

        // This is a bit ugly, the pathfinder should know about this
        Pathfinder.InvalidateCache();

        obstacle.Position.GetRegion().UpdateObstruction();
    }

    private void InitSpawnLocations() {
        StartingLocation = Controller.GetUnits(_unitsTracker.OwnedUnits, Units.TownHalls).First().Position.ToVector2();
        EnemyStartingLocation = Controller.GameInfo.StartRaw.StartLocations
            .Select(startLocation => new Vector2(startLocation.X, startLocation.Y))
            .MaxBy(enemyLocation => StartingLocation.DistanceTo(enemyLocation));
    }

    private static void InitHeightMap() {
        HeightMap = new List<List<float>>();
        for (var x = 0; x < MaxX; x++) {
            HeightMap.Add(new List<float>(new float[MaxY]));
        }

        var heightVector = Controller.GameInfo.StartRaw.TerrainHeight.Data
            .ToByteArray()
            .Select(ImageDataUtils.ByteToFloat)
            .ToList();

        for (var x = 0; x < MaxX; x++) {
            for (var y = 0; y < MaxY; y++) {
                HeightMap[x][y] = heightVector[y * MaxX + x]; // heightVector[4] is (4, 0)
            }
        }
    }

    private void InitTerrainWalkMap() {
        _terrainWalkMap = ParseWalkMap();

        // The walk data makes cells occupied by buildings impassable
        // However, if I want to find a path from my hatch to the enemy, the pathfinding will fail because the hatchery is impassable
        // Lucky for us, when we init the walk map, there's only 1 building so we'll make its cells walkable
        var startingTownHall = Controller.GetUnits(_unitsTracker.OwnedUnits, Units.Hatchery).First();
        var townHallCells = BuildSearchGrid(startingTownHall.Position, (int)startingTownHall.Radius);

        foreach (var cell in townHallCells) {
            _terrainWalkMap[(int)cell.X][(int)cell.Y] = true;
        }
    }

    private static void InitTerrainBuildMap() {
        _buildMap = new List<List<bool>>();
        for (var x = 0; x < MaxX; x++) {
            _buildMap.Add(new List<bool>(new bool[MaxY]));
        }

        var buildVector = Controller.GameInfo.StartRaw.PlacementGrid.Data
            .ToByteArray()
            .SelectMany(ImageDataUtils.ByteToBoolArray)
            .ToList();

        for (var x = 0; x < MaxX; x++) {
            for (var y = 0; y < MaxY; y++) {
                _buildMap[x][y] = buildVector[y * MaxX + x]; // walkVector[4] is (4, 0)
            }
        }
    }

    private static List<List<bool>> ParseWalkMap() {
        var walkMap = new List<List<bool>>();
        for (var x = 0; x < MaxX; x++) {
            walkMap.Add(new List<bool>(new bool[MaxY]));
        }

        var walkVector = Controller.GameInfo.StartRaw.PathingGrid.Data
            .ToByteArray()
            .SelectMany(ImageDataUtils.ByteToBoolArray)
            .ToList();

        for (var x = 0; x < MaxX; x++) {
            for (var y = 0; y < MaxY; y++) {
                walkMap[x][y] = walkVector[y * MaxX + x]; // walkVector[4] is (4, 0)

                // TODO GD This is problematic for _currentWalkMap
                // On some maps, some tiles under destructibles are not walkable
                // We'll consider them walkable, but they won't be until the obstacle is cleared
                if (ObstructionMap.Contains(new Vector2(x, y).AsWorldGridCenter())) {
                    walkMap[x][y] = true;
                }
            }
        }

        return walkMap;
    }

    public static IEnumerable<Vector2> BuildSearchGrid(Vector2 centerPosition, int gridRadius, float stepSize = KnowledgeBase.GameGridCellWidth) {
        var grid = new List<Vector2>();
        for (var x = centerPosition.X - gridRadius; x <= centerPosition.X + gridRadius; x += stepSize) {
            for (var y = centerPosition.Y - gridRadius; y <= centerPosition.Y + gridRadius; y += stepSize) {
                if (!IsInitialized || IsInBounds(x, y)) {
                    grid.Add(new Vector2(x, y));
                }
            }
        }

        return grid.OrderBy(position => centerPosition.DistanceTo(position));
    }

    public static IEnumerable<Vector3> BuildSearchGrid(Vector3 centerPosition, int gridRadius, float stepSize = KnowledgeBase.GameGridCellWidth) {
        var grid = new List<Vector3>();
        for (var x = centerPosition.X - gridRadius; x <= centerPosition.X + gridRadius; x += stepSize) {
            for (var y = centerPosition.Y - gridRadius; y <= centerPosition.Y + gridRadius; y += stepSize) {
                if (!IsInitialized || IsInBounds(x, y)) {
                    grid.Add(new Vector3(x, y, centerPosition.Z).WithWorldHeight());
                }
            }
        }

        return grid.OrderBy(position => Vector3.Distance(centerPosition, position));
    }

    // TODO GD Might not work with 2x2 buildings
    public static IEnumerable<Vector2> GetBuildingFootprint(Vector2 buildingCenter, uint buildingType) {
        return BuildSearchGrid(buildingCenter, (int)KnowledgeBase.GetBuildingRadius(buildingType));
    }

    /// <summary>
    /// Builds a search area composed of all the 1x1 game cells around a center position.
    /// The height is properly set on the returned cells.
    /// </summary>
    /// <param name="centerPosition">The position to search around</param>
    /// <param name="circleRadius">The radius of the search area</param>
    /// <param name="stepSize">The cells gap</param>
    /// <returns>The search area composed of all the 1x1 game cells around a center position with a stepSize sized gap</returns>
    public static IEnumerable<Vector2> BuildSearchRadius(Vector2 centerPosition, float circleRadius, float stepSize = KnowledgeBase.GameGridCellWidth) {
        return BuildSearchGrid(centerPosition, (int)circleRadius + 1, stepSize).Where(cell => cell.DistanceTo(centerPosition) <= circleRadius);
    }

    public static bool IsInBounds(Vector2 position) {
        return IsInBounds(position.X, position.Y);
    }

    public static bool IsInBounds(Vector3 position) {
        return IsInBounds(position.X, position.Y);
    }

    public static bool IsInBounds(float x, float y) {
        return x >= 0 && x < MaxX && y >= 0 && y < MaxY;
    }

    public static bool IsWalkable(Vector3 position, bool includeObstacles = true) {
        return IsWalkable(position.ToVector2(), includeObstacles);
    }

    public static bool IsWalkable(Vector2 position, bool includeObstacles = true) {
        if (!IsInBounds(position)) {
            return false;
        }

        var isWalkable = _terrainWalkMap[(int)position.X][(int)position.Y];
        var isObstructed = includeObstacles && ObstructionMap.Contains(position.AsWorldGridCenter());

        return isWalkable && !isObstructed;
    }

    public static bool IsBuildable(Vector3 position, bool includeObstacles = true) {
        return IsBuildable(position.ToVector2(), includeObstacles);
    }

    public static bool IsBuildable(Vector2 position, bool includeObstacles = true) {
        if (!IsInBounds(position)) {
            return false;
        }

        var isBuildable = _buildMap[(int)position.X][(int)position.Y];
        var isObstructed = includeObstacles && ObstructionMap.Contains(position.AsWorldGridCenter());

        return isBuildable && !isObstructed;
    }

    private static void InitWalkableCells() {
        for (var x = 0; x < MaxX; x++) {
            for (var y = 0; y < MaxY; y++) {
                var cell = new Vector2(x, y).AsWorldGridCenter();
                if (IsWalkable(cell)) {
                    _walkableCells.Add(cell);
                }
            }
        }
    }
}
