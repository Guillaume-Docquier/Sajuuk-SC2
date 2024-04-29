using System.Numerics;
using SC2APIProtocol;
using SC2Client.ExtensionMethods;
using SC2Client.GameData;

namespace SC2Client;

public class Terrain : ITerrain {
    private readonly FootprintCalculator _footprintCalculator;

    // TODO GD Benchmark if dict of Vector2 is slower than list of list
    private readonly Dictionary<Vector2, float> _heights;
    private readonly Dictionary<Vector2, bool> _obstructions;

    /// <summary>
    /// True for all cells that can be walked on.
    /// Obstructions are NOT taken into account here.
    /// </summary>
    private readonly Dictionary<Vector2, bool> _walkables;

    /// <summary>
    /// True for all cells that can be built on.
    /// Obstructions are NOT taken into account here.
    /// </summary>
    private readonly Dictionary<Vector2, bool> _buildables;

    public Terrain(FootprintCalculator footprintCalculator, ResponseGameInfo gameInfo) {
        _footprintCalculator = footprintCalculator;

        var totalCells = gameInfo.StartRaw.MapSize.X * gameInfo.StartRaw.MapSize.Y;

        _heights = new Dictionary<Vector2, float>(totalCells);
        InitHeights(gameInfo);

        _obstructions = new Dictionary<Vector2, bool>(totalCells);
        InitObstructions(gameInfo);

        _walkables = new Dictionary<Vector2, bool>(totalCells);
        InitWalkables(gameInfo);

        _buildables = new Dictionary<Vector2, bool>(totalCells);
        InitBuildables(gameInfo);
    }

    public void Update(ResponseObservation observation) {
        // TODO GD Update walkable based on cleared obstructions
    }

    public float GetHeight(Vector2 cell) {
        if (IsOutOfBounds(cell)) {
            return 0;
        }

        return _heights[cell.AsWorldGridCorner()];
    }

    public bool IsWalkable(Vector2 cell, bool considerObstructions = true) {
        if (IsOutOfBounds(cell)) {
            return false;
        }

        var cornerOfCell = cell.AsWorldGridCorner();
        if (considerObstructions && _obstructions.ContainsKey(cornerOfCell)) {
            return false;
        }

        return _walkables[cornerOfCell];
    }

    public bool IsBuildable(Vector2 cell, bool considerObstructions = true) {
        if (IsOutOfBounds(cell)) {
            return false;
        }

        var cornerOfCell = cell.AsWorldGridCorner();
        if (considerObstructions && _obstructions.ContainsKey(cornerOfCell)) {
            return false;
        }

        return _buildables[cornerOfCell];
    }

    public bool IsObstructed(Vector2 cell) {
        if (IsOutOfBounds(cell)) {
            return false;
        }

        return _obstructions[cell.AsWorldGridCorner()];
    }

    private void InitHeights(ResponseGameInfo gameInfo) {
        var heightVector = gameInfo.StartRaw.TerrainHeight.Data
            .ToByteArray()
            .Select(ImageDataUtils.ByteToFloat)
            .ToList();

        var maxX = gameInfo.StartRaw.MapSize.X;
        for (var x = 0; x < maxX; x++) {
            for (var y = 0; y < gameInfo.StartRaw.MapSize.Y; y++) {
                _heights[new Vector2(x, y)] = heightVector[y * maxX + x]; // heightVector[4] is (4, 0)
            }
        }
    }

    private void InitObstructions(ResponseGameInfo gameInfo) {
        var obstacleIds = new HashSet<uint>(UnitTypeId.Obstacles.Concat(UnitTypeId.MineralFields).Concat(UnitTypeId.GasGeysers));
        obstacleIds.Remove(UnitTypeId.UnbuildablePlatesDestructible); // It is destructible but you can walk on it

        var maxX = gameInfo.StartRaw.MapSize.X;
        for (var x = 0; x < maxX; x++) {
            for (var y = 0; y < gameInfo.StartRaw.MapSize.Y; y++) {
                _obstructions[new Vector2(x, y)] = false;
            }
        }

        _obstacles = _unitsTracker.GetUnits(_unitsTracker.NeutralUnits, obstacleIds).ToList();

        _obstacles.ForEach(obstacle => {
            obstacle.AddDeathWatcher(this);
            foreach (var cell in _footprintCalculator.GetFootprint(obstacle)) {
                // We ignore any footprint cell that is out of bounds
                if (_obstructions.ContainsKey(cell)) {
                    _obstructions[cell] = true;
                }
            }
        });
    }

    private void InitWalkables(ResponseGameInfo gameInfo) {
        var walkVector = gameInfo.StartRaw.PathingGrid.Data
            .ToByteArray()
            .SelectMany(ImageDataUtils.ByteToBoolArray)
            .ToList();

        var maxX = gameInfo.StartRaw.MapSize.X;
        for (var x = 0; x < maxX; x++) {
            for (var y = 0; y < gameInfo.StartRaw.MapSize.Y; y++) {
                var position = new Vector2(x, y);
                _walkables[position] = walkVector[y * maxX + x]; // walkVector[4] is (4, 0)

                // On some maps, some tiles under destructibles are not walkable
                // We'll consider them walkable, but they won't be until the obstacle is cleared
                if (_obstructions.ContainsKey(new Vector2(x, y))) {
                    _walkables[position] = true;
                }
            }
        }
    }

    private void InitBuildables(ResponseGameInfo gameInfo) {
        var buildVector = gameInfo.StartRaw.PlacementGrid.Data
            .ToByteArray()
            .SelectMany(ImageDataUtils.ByteToBoolArray)
            .ToList();

        // TODO GD We should ignore obstructions, but there's a problem
        // Rocks on ramps will never be buildable, but we can't make the difference without pre-computing the data offline.
        var maxX = gameInfo.StartRaw.MapSize.X;
        for (var x = 0; x < maxX; x++) {
            for (var y = 0; y < gameInfo.StartRaw.MapSize.Y; y++) {
                _buildables[new Vector2(x, y)] = buildVector[y * maxX + x]; // buildVector[4] is (4, 0)
            }
        }
    }

    /// <summary>
    /// Determines if a cell is within the bounds of the game.
    /// </summary>
    /// <param name="cell">The cell to query.</param>
    /// <returns>True if the cell is not within the game's bounds.</returns>
    private bool IsOutOfBounds(Vector2 cell) {
        // Walkable contains all playable cells. Anything that's not in there is not within bounds.
        return !_walkables.ContainsKey(cell.AsWorldGridCorner());
    }
}
