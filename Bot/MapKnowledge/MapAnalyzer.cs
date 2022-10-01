using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using SC2APIProtocol;

namespace Bot.MapKnowledge;

public class MapAnalyzer: INeedUpdating, IWatchUnitsDie {
    public static readonly MapAnalyzer Instance = new MapAnalyzer();
    public static bool IsInitialized = false;

    public static Vector3 StartingLocation;
    public static Vector3 EnemyStartingLocation;

    public static List<List<float>> HeightMap;

    private static List<Unit> _obstacles;
    private static readonly HashSet<Vector3> ObstructionMap = new HashSet<Vector3>();
    private static List<List<bool>> _terrainWalkMap;
    private static List<List<bool>> _currentWalkMap;

    public static int MaxX;
    public static int MaxY;

    private MapAnalyzer() {}

    public void Update(ResponseObservation observation) {
        _currentWalkMap = ParseWalkMap();

        if (IsInitialized) {
            return;
        }

        MaxX = Controller.GameInfo.StartRaw.MapSize.X;
        MaxY = Controller.GameInfo.StartRaw.MapSize.Y;

        InitSpawnLocations();
        InitObstacles();

        InitHeightMap();
        InitTerrainWalkMap();

        IsInitialized = true;
    }

    public static string GetStartingCorner() {
        var corners = new List<(Vector3 Position, string Name)>
        {
            (new Vector3(0, 0, 0), "bottom left"),
            (new Vector3(MaxX, 0, 0), "bottom right"),
            (new Vector3(0, MaxY, 0), "top left"),
            (new Vector3(MaxX, MaxY, 0), "top right"),
        };

        return corners.MinBy(corner => corner.Position.HorizontalDistanceTo(StartingLocation)).Name;
    }

    public void ReportUnitDeath(Unit deadUnit) {
        RemoveObstacle(deadUnit);
    }

    private static void InitObstacles() {
        var obstacleIds = new HashSet<uint>(Units.Obstacles.Concat(Units.MineralFields).Concat(Units.GasGeysers));
        obstacleIds.Remove(Units.UnbuildablePlatesDestructible); // It is destructible but you can walk on it

        _obstacles = Controller.GetUnits(UnitsTracker.NeutralUnits, obstacleIds).ToList();

        _obstacles.ForEach(obstacle => {
            obstacle.AddDeathWatcher(Instance);
            foreach (var cell in GetObstacleFootprint(obstacle)) {
                ObstructionMap.Add(cell);
            }
        });
    }

    private static void RemoveObstacle(Unit obstacle) {
        _obstacles.Remove(obstacle);
        foreach (var cell in GetObstacleFootprint(obstacle)) {
            ObstructionMap.Remove(cell);
        }

        Logger.Info("Obstacle removed, invalidating Pathfinder cache");

        // This is a big ugly, the pathfinder should know about this
        Pathfinder.Memory.Clear();
    }

    public static IEnumerable<Vector3> GetObstacleFootprint(Unit obstacle) {
        if (Units.MineralFields.Contains(obstacle.UnitType)) {
            // Mineral fields are 1x2
            return new List<Vector3>
            {
                obstacle.Position.Translate(xTranslation: -0.5f).AsWorldGridCenter().WithoutZ(),
                obstacle.Position.Translate(xTranslation: 0.5f).AsWorldGridCenter().WithoutZ(),
            };
        }

        // TODO GD Some debris are rectangular at an angle, so the grid is way bigger than it should be
        return BuildSearchGrid(obstacle.Position, (int)obstacle.Radius).Select(cell => cell.AsWorldGridCenter().WithoutZ());
    }

    private static void InitSpawnLocations() {
        StartingLocation = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.ResourceCenters).First().Position;
        EnemyStartingLocation = Controller.GameInfo.StartRaw.StartLocations
            .Select(startLocation => new Vector3(startLocation.X, startLocation.Y, 0))
            .MaxBy(enemyLocation => StartingLocation.HorizontalDistanceTo(enemyLocation));
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

    private static void InitTerrainWalkMap() {
        _terrainWalkMap = ParseWalkMap();

        // The walk data makes cells occupied by buildings impassable
        // However, if I want to find a path from my hatch to the enemy, the pathfinding will fail because the hatchery is impassable
        // Lucky for us, when we init the walk map, there's only 1 building so we'll make its cells walkable
        var startingTownHall = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery).First();
        var townHallCells = BuildSearchGrid(startingTownHall.Position, (int)startingTownHall.Radius);

        foreach (var cell in townHallCells) {
            _terrainWalkMap[(int)cell.X][(int)cell.Y] = true;
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
                if (ObstructionMap.Contains(new Vector3(x, y, 0).AsWorldGridCenter())) {
                    walkMap[x][y] = true;
                }
            }
        }

        return walkMap;
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

    public static IEnumerable<Vector3> BuildSearchRadius(Vector3 centerPosition, float circleRadius, float stepSize = KnowledgeBase.GameGridCellWidth) {
        return BuildSearchGrid(centerPosition, (int)circleRadius + 1, stepSize).Where(cell => cell.HorizontalDistanceTo(centerPosition) <= circleRadius);
    }

    public static bool IsInBounds(Vector3 position) {
        return IsInBounds(position.X, position.Y);
    }

    public static bool IsInBounds(float x, float y) {
        return x >= 0 && x < MaxX && y >= 0 && y < MaxY;
    }

    public static bool IsWalkable(Vector3 position, bool includeObstacles = true) {
        if (!IsInBounds(position)) {
            return false;
        }

        var isWalkable = _terrainWalkMap[(int)position.X][(int)position.Y];
        var isObstructed = includeObstacles && ObstructionMap.Contains(position.AsWorldGridCenter().WithoutZ());

        return isWalkable && !isObstructed;
    }
}
