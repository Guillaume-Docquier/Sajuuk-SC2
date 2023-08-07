using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sajuuk.ExtensionMethods;
using Sajuuk.Algorithms;
using Sajuuk.GameData;
using Sajuuk.MapAnalysis;
using SC2APIProtocol;

namespace Sajuuk.GameSense;

public class TerrainTracker : ITerrainTracker, INeedUpdating, IWatchUnitsDie {
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly IUnitsTracker _unitsTracker;
    private readonly KnowledgeBase _knowledgeBase;

    private readonly FootprintCalculator _footprintCalculator;

    private bool _isInitialized = false;

    public Vector2 StartingLocation { get; private set; }
    public Vector2 EnemyStartingLocation { get; private set; }

    public List<List<float>> HeightMap { get; private set; }

    private List<Unit> _obstacles;
    private readonly HashSet<Vector2> _obstructionMap = new HashSet<Vector2>();
    private List<List<bool>> _terrainWalkMap;
    private List<List<bool>> _currentWalkMap;
    private List<List<bool>> _buildMap;

    public int MaxX { get; private set; }
    public int MaxY { get; private set; }
    public float DiagonalLength { get; private set; }

    private readonly HashSet<Vector2> _walkableCells = new HashSet<Vector2>();
    public IReadOnlySet<Vector2> WalkableCells => _walkableCells;

    private readonly HashSet<Vector2> _playableCells = new HashSet<Vector2>();
    public IReadOnlySet<Vector2> PlayableCells => _playableCells;

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

    public TerrainTracker(
        IVisibilityTracker visibilityTracker,
        IUnitsTracker unitsTracker,
        KnowledgeBase knowledgeBase
    ) {
        _visibilityTracker = visibilityTracker;
        _unitsTracker = unitsTracker;
        _knowledgeBase = knowledgeBase;

        _footprintCalculator = new FootprintCalculator(this);
    }

    public void Update(ResponseObservation observation, ResponseGameInfo gameInfo) {
        if (_isInitialized) {
            _currentWalkMap = ParseWalkMap(gameInfo);
            return;
        }

        MaxX = gameInfo.StartRaw.MapSize.X;
        MaxY = gameInfo.StartRaw.MapSize.Y;
        DiagonalLength = (float)Math.Sqrt(MaxX * MaxX + MaxY * MaxY);

        _currentWalkMap = ParseWalkMap(gameInfo);

        InitSpawnLocations(gameInfo);
        InitObstacles();

        InitHeightMap(gameInfo);
        InitTerrainWalkMap(gameInfo);
        InitTerrainBuildMap(gameInfo);

        InitCells();

        _isInitialized = true;
    }

    public string GetStartingCorner() {
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

        _obstacles = _unitsTracker.GetUnits(_unitsTracker.NeutralUnits, obstacleIds).ToList();

        _obstacles.ForEach(obstacle => {
            obstacle.AddDeathWatcher(this);
            foreach (var cell in _footprintCalculator.GetFootprint(obstacle)) {
                _obstructionMap.Add(cell);
            }
        });
    }

    private void RemoveObstacle(Unit obstacle) {
        _obstacles.Remove(obstacle);
        foreach (var cell in _footprintCalculator.GetFootprint(obstacle)) {
            _obstructionMap.Remove(cell);
        }
    }

    private void InitSpawnLocations(ResponseGameInfo gameInfo) {
        StartingLocation = _unitsTracker.GetUnits(_unitsTracker.OwnedUnits, Units.TownHalls).First().Position.ToVector2();
        EnemyStartingLocation = gameInfo.StartRaw.StartLocations
            .Select(startLocation => new Vector2(startLocation.X, startLocation.Y))
            .MaxBy(enemyLocation => StartingLocation.DistanceTo(enemyLocation));
    }

    private void InitHeightMap(ResponseGameInfo gameInfo) {
        HeightMap = new List<List<float>>();
        for (var x = 0; x < MaxX; x++) {
            HeightMap.Add(new List<float>(new float[MaxY]));
        }

        var heightVector = gameInfo.StartRaw.TerrainHeight.Data
            .ToByteArray()
            .Select(ImageDataUtils.ByteToFloat)
            .ToList();

        for (var x = 0; x < MaxX; x++) {
            for (var y = 0; y < MaxY; y++) {
                HeightMap[x][y] = heightVector[y * MaxX + x]; // heightVector[4] is (4, 0)
            }
        }
    }

    private void InitTerrainWalkMap(ResponseGameInfo gameInfo) {
        _terrainWalkMap = ParseWalkMap(gameInfo);

        // The walk data makes cells occupied by buildings impassable
        // However, if I want to find a path from my hatch to the enemy, the pathfinding will fail because the hatchery is impassable
        // Lucky for us, when we init the walk map, there's only 1 building so we'll make its cells walkable
        var startingTownHall = _unitsTracker.GetUnits(_unitsTracker.OwnedUnits, Units.Hatchery).First();
        var townHallCells = BuildSearchGrid(startingTownHall.Position, (int)startingTownHall.Radius);

        foreach (var cell in townHallCells) {
            _terrainWalkMap[(int)cell.X][(int)cell.Y] = true;
        }
    }

    private void InitTerrainBuildMap(ResponseGameInfo gameInfo) {
        _buildMap = new List<List<bool>>();
        for (var x = 0; x < MaxX; x++) {
            _buildMap.Add(new List<bool>(new bool[MaxY]));
        }

        var buildVector = gameInfo.StartRaw.PlacementGrid.Data
            .ToByteArray()
            .SelectMany(ImageDataUtils.ByteToBoolArray)
            .ToList();

        for (var x = 0; x < MaxX; x++) {
            for (var y = 0; y < MaxY; y++) {
                _buildMap[x][y] = buildVector[y * MaxX + x]; // walkVector[4] is (4, 0)
            }
        }
    }

    private List<List<bool>> ParseWalkMap(ResponseGameInfo gameInfo) {
        var walkMap = new List<List<bool>>();
        for (var x = 0; x < MaxX; x++) {
            walkMap.Add(new List<bool>(new bool[MaxY]));
        }

        var walkVector = gameInfo.StartRaw.PathingGrid.Data
            .ToByteArray()
            .SelectMany(ImageDataUtils.ByteToBoolArray)
            .ToList();

        for (var x = 0; x < MaxX; x++) {
            for (var y = 0; y < MaxY; y++) {
                walkMap[x][y] = walkVector[y * MaxX + x]; // walkVector[4] is (4, 0)

                // TODO GD This is problematic for _currentWalkMap
                // On some maps, some tiles under destructibles are not walkable
                // We'll consider them walkable, but they won't be until the obstacle is cleared
                if (_obstructionMap.Contains(new Vector2(x, y).AsWorldGridCenter())) {
                    walkMap[x][y] = true;
                }
            }
        }

        return walkMap;
    }

    public IEnumerable<Vector2> BuildSearchGrid(Vector2 centerPosition, int gridRadius, float stepSize = KnowledgeBase.GameGridCellWidth) {
        var grid = new List<Vector2>();
        for (var x = centerPosition.X - gridRadius; x <= centerPosition.X + gridRadius; x += stepSize) {
            for (var y = centerPosition.Y - gridRadius; y <= centerPosition.Y + gridRadius; y += stepSize) {
                if (!_isInitialized || IsInBounds(x, y)) {
                    grid.Add(new Vector2(x, y));
                }
            }
        }

        return grid.OrderBy(position => centerPosition.DistanceTo(position));
    }

    public IEnumerable<Vector3> BuildSearchGrid(Vector3 centerPosition, int gridRadius, float stepSize = KnowledgeBase.GameGridCellWidth) {
        var grid = new List<Vector3>();
        for (var x = centerPosition.X - gridRadius; x <= centerPosition.X + gridRadius; x += stepSize) {
            for (var y = centerPosition.Y - gridRadius; y <= centerPosition.Y + gridRadius; y += stepSize) {
                if (!_isInitialized || IsInBounds(x, y)) {
                    grid.Add(WithWorldHeight(new Vector3(x, y, centerPosition.Z)));
                }
            }
        }

        return grid.OrderBy(position => Vector3.Distance(centerPosition, position));
    }

    // TODO GD Might not work with 2x2 buildings
    public IEnumerable<Vector2> GetBuildingFootprint(Vector2 buildingCenter, uint buildingType) {
        return BuildSearchGrid(buildingCenter, (int)_knowledgeBase.GetBuildingRadius(buildingType));
    }

    /// <summary>
    /// Builds a search area composed of all the 1x1 game cells around a center position.
    /// The height is properly set on the returned cells.
    /// </summary>
    /// <param name="centerPosition">The position to search around</param>
    /// <param name="circleRadius">The radius of the search area</param>
    /// <param name="stepSize">The cells gap</param>
    /// <returns>The search area composed of all the 1x1 game cells around a center position with a stepSize sized gap</returns>
    public IEnumerable<Vector2> BuildSearchRadius(Vector2 centerPosition, float circleRadius, float stepSize = KnowledgeBase.GameGridCellWidth) {
        return BuildSearchGrid(centerPosition, (int)circleRadius + 1, stepSize).Where(cell => cell.DistanceTo(centerPosition) <= circleRadius);
    }

    public bool IsInBounds(Vector2 position) {
        return IsInBounds(position.X, position.Y);
    }

    public bool IsInBounds(Vector3 position) {
        return IsInBounds(position.X, position.Y);
    }

    public bool IsInBounds(float x, float y) {
        return x >= 0 && x < MaxX && y >= 0 && y < MaxY;
    }

    public bool IsWalkable(Vector3 position, bool considerObstaclesObstructions = true) {
        return IsWalkable(position.ToVector2(), considerObstaclesObstructions);
    }

    public bool IsWalkable(Vector2 position, bool considerObstaclesObstructions = true) {
        if (!IsInBounds(position)) {
            return false;
        }

        var isWalkable = _terrainWalkMap[(int)position.X][(int)position.Y];
        var isObstructed = considerObstaclesObstructions && _obstructionMap.Contains(position.AsWorldGridCenter());

        return isWalkable && !isObstructed;
    }

    public bool IsBuildable(Vector3 position, bool considerObstaclesObstructions = true) {
        return IsBuildable(position.ToVector2(), considerObstaclesObstructions);
    }

    public bool IsBuildable(Vector2 position, bool considerObstaclesObstructions = true) {
        if (!IsInBounds(position)) {
            return false;
        }

        var isBuildable = _buildMap[(int)position.X][(int)position.Y];
        var isObstructed = considerObstaclesObstructions && _obstructionMap.Contains(position.AsWorldGridCenter());

        return isBuildable && !isObstructed;
    }

    /// <summary>
    /// <para>Gets up to 8 reachable neighbors around the position.</para>
    /// <para>Top, left, down and right are given if they are walkable.</para>
    /// <para>
    /// Diagonal neighbors are returned unless both of their components are obstructed.
    /// For example, the top right diagonal is reachable unless both the top and the right are obstructed.
    /// </para>
    /// <para>Obstructed is not to be confused with walkable. Units can take a diagonal path between two unwalkable cells, but not between obstructed cells.</para>
    /// <para>This is a game detail.</para>
    /// </summary>
    /// <param name="position">The position to get the neighbors of.</param>
    /// <param name="allowedNeighborhood">The cells that are allowed to be neighbors. Defaults to all cells in the map.</param>
    /// <param name="considerObstaclesObstructions">Whether to consider obstacles when deciding if a cell is walkable.</param>
    /// <returns>Up to 8 neighbors</returns>
    public IEnumerable<Vector2> GetReachableNeighbors(Vector2 position, IReadOnlySet<Vector2> allowedNeighborhood = null, bool considerObstaclesObstructions = true) {
        var leftPos = position.Translate(xTranslation: -1);
        var isLeftReachable = IsWalkableNeighbor(leftPos, considerObstaclesObstructions, allowedNeighborhood);
        if (isLeftReachable) {
            yield return leftPos;
        }

        var rightPos = position.Translate(xTranslation: 1);
        var isRightReachable = IsWalkableNeighbor(rightPos, considerObstaclesObstructions, allowedNeighborhood);
        if (isRightReachable) {
            yield return rightPos;
        }

        var upPos = position.Translate(yTranslation: 1);
        var isUpReachable = IsWalkableNeighbor(upPos, considerObstaclesObstructions, allowedNeighborhood);
        if (isUpReachable) {
            yield return upPos;
        }

        var downPos = position.Translate(yTranslation: -1);
        var isDownReachable = IsWalkableNeighbor(downPos, considerObstaclesObstructions, allowedNeighborhood);
        if (isDownReachable) {
            yield return downPos;
        }

        if (!considerObstaclesObstructions || !IsDiagonalObstruction(leftPos, upPos)) {
            var leftUpPos = position.Translate(xTranslation: -1, yTranslation: 1);
            if (IsWalkableNeighbor(leftUpPos, considerObstaclesObstructions, allowedNeighborhood)) {
                yield return leftUpPos;
            }
        }

        if (!considerObstaclesObstructions || !IsDiagonalObstruction(leftPos, downPos)) {
            var leftDownPos = position.Translate(xTranslation: -1, yTranslation: -1);
            if (IsWalkableNeighbor(leftDownPos, considerObstaclesObstructions, allowedNeighborhood)) {
                yield return leftDownPos;
            }
        }

        if (!considerObstaclesObstructions || !IsDiagonalObstruction(rightPos, upPos)) {
            var rightUpPos = position.Translate(xTranslation: 1, yTranslation: 1);
            if (IsWalkableNeighbor(rightUpPos, considerObstaclesObstructions, allowedNeighborhood)) {
                yield return rightUpPos;
            }
        }

        if (!considerObstaclesObstructions || !IsDiagonalObstruction(rightPos, downPos)) {
            var rightDownPos = position.Translate(xTranslation: 1, yTranslation: -1);
            if (IsWalkableNeighbor(rightDownPos, considerObstaclesObstructions, allowedNeighborhood)) {
                yield return rightDownPos;
            }
        }
    }

    /// <summary>
    /// Determines is the given position is a walkable neighbor.
    /// </summary>
    /// <param name="pos">The position to check</param>
    /// <param name="considerObstaclesObstructions">Whether to consider obstacles when deciding if a cell is walkable.</param>
    /// <param name="allowedNeighborhood">The allowed neighbors. Does nothing if not specified.</param>
    /// <returns>True if the cell is a walkable, allowed neighbor.</returns>
    private bool IsWalkableNeighbor(Vector2 pos, bool considerObstaclesObstructions, IReadOnlySet<Vector2> allowedNeighborhood = null) {
        if (allowedNeighborhood != null && !allowedNeighborhood.Contains(pos)) {
            return false;
        }

        return IsWalkable(pos, considerObstaclesObstructions);
    }

    /// <summary>
    /// Determines if the given cells would cause a diagonal obstruction.
    /// It is assumed that that the given cells form a continuous diagonal line.
    /// An obstruction occurs if all walkable obstructingCells are obstructed.
    /// </summary>
    /// <param name="obstructingCells">The cells that might cause an obstruction.</param>
    /// <returns>True if the given cells would prevent movement.</returns>
    private bool IsDiagonalObstruction(params Vector2[] obstructingCells) {
        return obstructingCells
            .Where(cell => IsWalkable(cell, considerObstaclesObstructions: false))
            .All(cell => _obstructionMap.Contains(cell));
    }

    public Vector2 GetClosestWalkable(Vector2 position, int searchRadius = 8, HashSet<Vector2> allowedCells = null) {
        if (IsWalkable(position)) {
            return position;
        }

        var searchGrid = BuildSearchGrid(position, searchRadius)
            .Where(cell => IsWalkable(cell));

        if (allowedCells != null) {
            searchGrid = searchGrid.Where(allowedCells.Contains);
        }

        var closestWalkableCell = searchGrid
            .DefaultIfEmpty()
            .MinBy(cell => cell.DistanceTo(position));

        // It's probably good to avoid returning default?
        if (closestWalkableCell == default) {
            Logger.Error("Vector3.ClosestWalkable returned no elements in a 15 radius around {0}", position);
            return position;
        }

        return closestWalkableCell;
    }

    public Vector3 GetClosestWalkable(Vector3 position) {
        if (IsWalkable(position)) {
            return position;
        }

        var closestWalkableCell = BuildSearchGrid(position, 15)
            .Where(cell => IsWalkable(cell))
            .DefaultIfEmpty()
            .MinBy(cell => cell.HorizontalDistanceTo(position));

        // It's probably good to avoid returning default?
        if (closestWalkableCell == default) {
            Logger.Error("Vector3.ClosestWalkable returned no elements in a 15 radius around {0}", position);
            return position;
        }

        return closestWalkableCell;
    }

    public Vector3 WithWorldHeight(Vector3 vector, float zOffset = 0) {
        if (!_isInitialized) {
            return vector;
        }

        if (!IsInBounds(vector)) {
            return vector;
        }

        // Some unwalkable cells are low on the map, let's try to bring them up if they touch a walkable cell (that generally have proper heights)
        if (!IsWalkable(vector)) {
            var walkableNeighbors = vector.GetNeighbors().Where(neighbor => IsWalkable(neighbor)).ToList();
            if (walkableNeighbors.Any()) {
                return vector with { Z = walkableNeighbors.Max(neighbor => WithWorldHeight(neighbor).Z) };
            }
        }

        return vector with { Z = HeightMap[(int)vector.X][(int)vector.Y] + zOffset };
    }

    public Vector3 WithWorldHeight(Vector2 vector, float zOffset = 0f) {
        var vector3 = new Vector3(vector, zOffset);

        return WithWorldHeight(vector3, zOffset);
    }

    /// <summary>
    /// Gets all cells traversed by the ray from origin to destination using digital differential analyzer (DDA)
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="destination"></param>
    /// <returns>The cells traversed by the ray from origin to destination</returns>
    public HashSet<Vector3> GetPointsInBetween(Vector3 origin, Vector3 destination) {
        var targetCellCorner = destination.ToVector2().AsWorldGridCorner();

        var pointsInBetween = RayCasting.RayCast(origin.ToVector2(), destination.ToVector2(), cellCorner => cellCorner == targetCellCorner)
            .Select(result => WithWorldHeight(result.CornerOfCell.AsWorldGridCenter()))
            .ToHashSet();

        return pointsInBetween;
    }

    /// <summary>
    /// Initializes playableCells and walkableCells.
    /// </summary>
    private void InitCells() {
        for (var x = 0; x < MaxX; x++) {
            for (var y = 0; y < MaxY; y++) {
                var cell = new Vector2(x, y).AsWorldGridCenter();
                if (IsWalkable(cell, considerObstaclesObstructions: false)) {
                    _playableCells.Add(cell);

                    if (IsWalkable(cell, considerObstaclesObstructions: true)) {
                        _walkableCells.Add(cell);
                    }
                }
            }
        }
    }
}
