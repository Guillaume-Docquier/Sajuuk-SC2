using System.Numerics;
using Algorithms.ExtensionMethods;
using SC2Client.ExtensionMethods;
using SC2Client.GameData;
using SC2Client.PubSub.Events;
using SC2Client.State;

namespace SC2Client.Trackers;

public class TerrainTracker : ITracker, ITerrainTracker {
    private readonly FootprintCalculator _footprintCalculator;
    private readonly IUnitsTracker _unitsTracker;
    private readonly ILogger _logger;

    private bool _isInitialized = false;

    /// <summary>
    /// Will not be null once _isInitialized is true
    /// </summary>
    private ITerrain _terrain = null!;

    /// <summary>
    /// The cells are expressed as their corner.
    /// </summary>
    private readonly HashSet<Vector2> _obstructedCells = new HashSet<Vector2>();
    private List<IUnit> _obstacles = new List<IUnit>();

    public TerrainTracker(FootprintCalculator footprintCalculator, IUnitsTracker unitsTracker, ILogger logger) {
        _footprintCalculator = footprintCalculator;
        _unitsTracker = unitsTracker;
        _logger = logger.CreateNamed("TerrainTracker");
    }

    public void Update(IGameState gameState) {
        _terrain = gameState.Terrain;

        if (_isInitialized) {
            return;
        }

        InitObstacles();

        _isInitialized = true;
    }

    public int MaxX => _terrain.MaxX;
    public int MaxY => _terrain.MaxY;

    public IEnumerable<Vector2> ObstructedCells => _obstructedCells;

    public Vector3 WithWorldHeight(Vector2 cell, float zOffset = 0) {
        if (!IsWithinBounds(cell)) {
            return new Vector3(cell.X, cell.Y, zOffset);
        }

        // Some unwalkable cells are low on the map, let's try to bring them up if they touch a walkable cell (that generally have proper heights)
        if (!IsWalkable(cell)) {
            var walkableNeighbors = cell.GetNeighbors().Where(neighbor => IsWalkable(neighbor)).ToList();
            if (walkableNeighbors.Any()) {
                return new Vector3(cell.X, cell.Y, walkableNeighbors.Max(neighbor => WithWorldHeight(neighbor).Z) + zOffset);
            }
        }

        return new Vector3(cell.X, cell.Y, _terrain.CellHeights[cell.AsWorldGridCorner()] + zOffset);
    }

    public bool IsWalkable(Vector2 cell, bool considerObstructions = true) {
        if (!IsWithinBounds(cell)) {
            return false;
        }

        if (considerObstructions && IsObstructed(cell)) {
            return false;
        }

        return _terrain.WalkableCells.Contains(cell.AsWorldGridCorner());
    }

    public bool IsBuildable(Vector2 cell, bool considerObstructions = true) {
        if (!IsWithinBounds(cell)) {
            return false;
        }

        if (considerObstructions && IsObstructed(cell)) {
            return false;
        }

        return _terrain.BuildableCells.Contains(cell.AsWorldGridCorner());
    }

    public bool IsObstructed(Vector2 cell) {
        return _obstructedCells.Contains(cell.AsWorldGridCorner());
    }

    public bool IsWithinBounds(Vector2 position) {
        return position.X >= 0 && position.X < _terrain.MaxX && position.Y >= 0 && position.Y < _terrain.MaxY;
    }

    public IEnumerable<Vector2> GetReachableNeighbors(Vector2 cell, IReadOnlySet<Vector2>? potentialNeighbors = null, bool considerObstructions = true) {
        bool IsReachable(Vector2 pos) {
            if (potentialNeighbors != null && !potentialNeighbors.Contains(pos)) {
                return false;
            }

            return IsWithinBounds(pos) && IsWalkable(pos, considerObstructions);
        }

        var leftPos = cell.Translate(xTranslation: -1);
        var isLeftReachable = IsReachable(leftPos);
        if (isLeftReachable) {
            yield return leftPos;
        }

        var rightPos = cell.Translate(xTranslation: 1);
        var isRightReachable = IsReachable(rightPos);
        if (isRightReachable) {
            yield return rightPos;
        }

        var upPos = cell.Translate(yTranslation: 1);
        var isUpReachable = IsReachable(upPos);
        if (isUpReachable) {
            yield return upPos;
        }

        var downPos = cell.Translate(yTranslation: -1);
        var isDownReachable = IsReachable(downPos);
        if (isDownReachable) {
            yield return downPos;
        }

        if (isLeftReachable || isUpReachable) {
            var leftUpPos = cell.Translate(xTranslation: -1, yTranslation: 1);
            if (IsReachable(leftUpPos)) {
                yield return leftUpPos;
            }
        }

        if (isLeftReachable || isDownReachable) {
            var leftDownPos = cell.Translate(xTranslation: -1, yTranslation: -1);
            if (IsReachable(leftDownPos)) {
                yield return leftDownPos;
            }
        }

        if (isRightReachable || isUpReachable) {
            var rightUpPos = cell.Translate(xTranslation: 1, yTranslation: 1);
            if (IsReachable(rightUpPos)) {
                yield return rightUpPos;
            }
        }

        if (isRightReachable || isDownReachable) {
            var rightDownPos = cell.Translate(xTranslation: 1, yTranslation: -1);
            if (IsReachable(rightDownPos)) {
                yield return rightDownPos;
            }
        }
    }

    public Vector2 GetClosestWalkable(Vector2 position, int searchRadius = 8, HashSet<Vector2>? allowedCells = null) {
        if (IsWalkable(position)) {
            return position;
        }

        var searchGrid = position.AsWorldGridCorner().BuildSearchGrid(searchRadius)
            .Where(cell => IsWalkable(cell));

        if (allowedCells != null) {
            searchGrid = searchGrid.Where(allowedCells.Contains);
        }

        var closestWalkableCell = searchGrid
            .DefaultIfEmpty()
            .MinBy(cell => cell.DistanceTo(position));

        // It's probably good to avoid returning default?
        if (closestWalkableCell == default) {
            _logger.Error($"GetClosestWalkable returned no elements in a {searchRadius} radius around {position}");
            return position;
        }

        return closestWalkableCell;
    }

    private void InitObstacles() {
        var obstacleIds = new HashSet<uint>(UnitTypeId.Obstacles.Concat(UnitTypeId.MineralFields).Concat(UnitTypeId.GasGeysers));
        obstacleIds.Remove(UnitTypeId.UnbuildablePlatesDestructible); // It is destructible but you can walk on it

        _obstacles = UnitQueries.GetUnits(_unitsTracker.NeutralUnits, obstacleIds).ToList();
        _obstacles.ForEach(obstacle => {
            obstacle.Register(OnObstacleRemoved);
            foreach (var cell in _footprintCalculator.GetFootprint(obstacle)) {
                _obstructedCells.Add(cell.AsWorldGridCorner());
            }
        });
    }

    private void OnObstacleRemoved(UnitDeath unitDeath) {
        _obstacles.Remove(unitDeath.unit);
        foreach (var cell in _footprintCalculator.GetFootprint(unitDeath.unit)) {
            _obstructedCells.Remove(cell);
        }
    }
}
