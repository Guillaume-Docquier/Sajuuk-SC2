using System.Numerics;
using SC2APIProtocol;

namespace SC2Client.State;

public class Terrain : ITerrain {
    private readonly Dictionary<Vector2, float> _cellHeights;
    private readonly HashSet<Vector2> _cells;
    private readonly HashSet<Vector2> _walkableCells;
    private readonly HashSet<Vector2> _buildableCells;

    public int MaxX { get; }
    public int MaxY { get; }
    public IReadOnlyDictionary<Vector2, float> CellHeights => _cellHeights;
    public IReadOnlySet<Vector2> Cells => _cells;
    public IReadOnlySet<Vector2> WalkableCells => _walkableCells;
    public IReadOnlySet<Vector2> BuildableCells => _buildableCells;

    public Terrain(ResponseGameInfo gameInfo) {
        MaxX = gameInfo.StartRaw.MapSize.X;
        MaxY = gameInfo.StartRaw.MapSize.Y;

        _cellHeights = InitCellHeights(gameInfo);
        _walkableCells = InitWalkableCells(gameInfo);
        _cells = _walkableCells.ToHashSet(); // At the start of the game, all playable cells are walkable because no obstructions are in direct sight.
        _buildableCells = InitBuildableCells(gameInfo);
    }

    public void Update(ResponseObservation observation) {
        // TODO GD Update walkables/buildables?
    }

    private static Dictionary<Vector2, float> InitCellHeights(ResponseGameInfo gameInfo) {
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

    private static HashSet<Vector2> InitWalkableCells(ResponseGameInfo gameInfo) {
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

    private static HashSet<Vector2> InitBuildableCells(ResponseGameInfo gameInfo) {
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
}
