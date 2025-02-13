using System.Numerics;
using System.Text.Json.Serialization;
using SC2APIProtocol;
using SC2Client.ExtensionMethods;
using SC2Client.GameData;

namespace SC2Client.State;

public class Terrain : ITerrain {
    private readonly FootprintCalculator _footprintCalculator;

    [JsonInclude] public Dictionary<Vector2, float> _cellHeights { get; init; }
    [JsonInclude] public HashSet<Vector2> _cells { get; init; }

    [JsonInclude] public HashSet<Vector2> _obstructedCells { get; private set; }
    [JsonInclude] public HashSet<Vector2> _walkableCells { get; private set; }
    [JsonInclude] public HashSet<Vector2> _buildableCells { get; private set; }

    public int MaxX { get; }
    public int MaxY { get; }

    [JsonIgnore] public IReadOnlyDictionary<Vector2, float> CellHeights => _cellHeights;
    [JsonIgnore] public IReadOnlySet<Vector2> Cells => _cells;
    [JsonIgnore] public IReadOnlySet<Vector2> ObstructedCells => _obstructedCells;
    [JsonIgnore] public IReadOnlySet<Vector2> WalkableCells => _walkableCells;
    [JsonIgnore] public IReadOnlySet<Vector2> BuildableCells => _buildableCells;

    [JsonConstructor]
    [Obsolete("Do not use this parameterless JsonConstructor", error: true)]
#pragma warning disable CS8618, CS9264
    public Terrain() {}
#pragma warning restore CS8618, CS9264

    public Terrain(FootprintCalculator footprintCalculator, ResponseGameInfo gameInfo, Units units) {
        _footprintCalculator = footprintCalculator;
        _obstructedCells = new HashSet<Vector2>();
        _walkableCells = new HashSet<Vector2>();
        _buildableCells = new HashSet<Vector2>();

        MaxX = gameInfo.StartRaw.MapSize.X;
        MaxY = gameInfo.StartRaw.MapSize.Y;

        _cellHeights = ParseCellHeights(gameInfo);

        Update(gameInfo, units);

        _cells = _walkableCells.ToHashSet();
        _cells.UnionWith(ComputeStartingBuildingCells(units));
        _cells.UnionWith(_obstructedCells);
    }

    /// <summary>
    /// Updates the obstructed, walkable and buildable cells.
    /// </summary>
    /// <param name="gameInfo"></param>
    /// <param name="units"></param>
    public void Update(ResponseGameInfo gameInfo, Units units) {
        // The game state is imperfect.
        // Cells are walkable even if some obstacles are on them, because they could die in the fog of war.
        // However, some obstacles are not walkable, even in the fog of war.
        // We have to adjust this ourselves.
        // There is also not a list of cells that would be walkable if all units were removed, so we have to compute it ourselves.
        _obstructedCells = ComputeObstructedCells(units);

        _walkableCells = ParseWalkableCells(gameInfo);
        // _walkableCells.ExceptWith(_obstructedCells);

        _buildableCells = ParseBuildableCells(gameInfo);
        // _buildableCells.ExceptWith(_obstructedCells);
    }

    private static Dictionary<Vector2, float> ParseCellHeights(ResponseGameInfo gameInfo) {
        var heightVector = gameInfo.StartRaw.TerrainHeight.Data
            .ToByteArray()
            .Select(ImageDataUtils.ByteToFloat)
            .ToList();

        var heights = new Dictionary<Vector2, float>();
        var maxX = gameInfo.StartRaw.MapSize.X;
        for (var x = 0; x < maxX; x++) {
            for (var y = 0; y < gameInfo.StartRaw.MapSize.Y; y++) {
                heights[new Vector2(x, y)] = heightVector[y * maxX + x]; // heightVector[4] is (4, 0)
            }
        }

        return heights;
    }

    private HashSet<Vector2> ComputeObstructedCells(Units units) {
        var obstacleIds = new HashSet<uint>(UnitTypeId.Obstacles.Concat(UnitTypeId.MineralFields).Concat(UnitTypeId.GasGeysers));
        obstacleIds.Remove(UnitTypeId.UnbuildablePlatesDestructible); // It is destructible but you can walk on it

        return UnitQueries.GetUnits(units.NeutralUnits, obstacleIds)
            .SelectMany(_footprintCalculator.GetFootprint)
            .Select(position => position.AsWorldGridCorner())
            .ToHashSet();
    }

    private static HashSet<Vector2> ParseWalkableCells(ResponseGameInfo gameInfo) {
        var walkVector = gameInfo.StartRaw.PathingGrid.Data
            .ToByteArray()
            .SelectMany(ImageDataUtils.ByteToBoolArray)
            .ToList();

        var walkableCells = new HashSet<Vector2>();
        var maxX = gameInfo.StartRaw.MapSize.X;
        for (var x = 0; x < maxX; x++) {
            for (var y = 0; y < gameInfo.StartRaw.MapSize.Y; y++) {
                // walkVector[4] is (4, 0)
                if (walkVector[y * maxX + x]) {
                    walkableCells.Add(new Vector2(x, y));
                }
            }
        }

        return walkableCells;
    }

    private static HashSet<Vector2> ParseBuildableCells(ResponseGameInfo gameInfo) {
        var buildVector = gameInfo.StartRaw.PlacementGrid.Data
            .ToByteArray()
            .SelectMany(ImageDataUtils.ByteToBoolArray)
            .ToList();

        var buildableCells = new HashSet<Vector2>();
        var maxX = gameInfo.StartRaw.MapSize.X;
        for (var x = 0; x < maxX; x++) {
            for (var y = 0; y < gameInfo.StartRaw.MapSize.Y; y++) {
                // buildVector[4] is (4, 0)
                if (buildVector[y * maxX + x]) {
                    buildableCells.Add(new Vector2(x, y));
                }
            }
        }

        return buildableCells;
    }

    private IEnumerable<Vector2> ComputeStartingBuildingCells(Units units) {
        return UnitQueries.GetUnits(units.OwnedUnits, UnitTypeId.Buildings)
            .SelectMany(_footprintCalculator.GetFootprint)
            .Select(position => position.AsWorldGridCorner());
    }
}
