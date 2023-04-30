using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Debugging;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.MapAnalysis.ExpandAnalysis;
using Bot.MapAnalysis.RegionAnalysis;
using Bot.Persistence;
using SC2APIProtocol;

namespace Bot.GameSense;

public class RegionsTracker : IRegionsTracker, INeedUpdating, IWatchUnitsDie {
    /// <summary>
    /// DI: ✔️ The only usages are for static instance creations
    /// </summary>
    public static readonly RegionsTracker Instance = new RegionsTracker(
        TerrainTracker.Instance,
        DebuggingFlagsTracker.Instance,
        new RegionsDataRepository(Program.MapFileName),
        ExpandUnitsAnalyzer.Instance,
        UnitsTracker.Instance
    );

    private readonly ITerrainTracker _terrainTracker;
    private readonly IDebuggingFlagsTracker _debuggingFlagsTracker;
    private readonly IMapDataRepository<RegionsData> _regionsRepository;
    private readonly IExpandUnitsAnalyzer _expandUnitsAnalyzer;
    private readonly IUnitsTracker _unitsTracker;

    private const int ExpandRadius = 3; // It's 2.5, we put 3 to be safe

    private Dictionary<Vector2, IRegion> _regionsLookupMap;
    private RegionsData _regionsData;


    private List<ExpandLocation> _expandLocations;
    private Dictionary<ExpandType, List<ExpandLocation>> _expandLocationsByType;

    public IEnumerable<IExpandLocation> ExpandLocations => _expandLocations;
    public IEnumerable<IRegion> Regions => _regionsData.Regions;

    public RegionsTracker(
        ITerrainTracker terrainTracker,
        IDebuggingFlagsTracker debuggingFlagsTracker,
        IMapDataRepository<RegionsData> regionsRepository,
        IExpandUnitsAnalyzer expandUnitsAnalyzer,
        IUnitsTracker unitsTracker
    ) {
        _terrainTracker = terrainTracker;
        _debuggingFlagsTracker = debuggingFlagsTracker;
        _regionsRepository = regionsRepository;
        _expandUnitsAnalyzer = expandUnitsAnalyzer;
        _unitsTracker = unitsTracker;
    }

    public void Reset() {
        // TODO GD Load the data before the game starts
        _regionsData = _regionsRepository.Load();

        _regionsLookupMap = BuildRegionsLookupMap(_regionsData.Regions);

        _expandLocations = _regionsData.Regions
            .Select(region => region.ConcreteExpandLocation)
            .Where(expandLocation => expandLocation != null)
            .ToList();

        _expandLocationsByType = _expandLocations
            .GroupBy(expandLocation => expandLocation.ExpandType)
            .ToDictionary(group => group.Key, group => group.ToList());

        foreach (var expandLocation in _expandLocations) {
            expandLocation.SetResources(_expandUnitsAnalyzer.FindExpandResources(expandLocation.Position));
            expandLocation.SetBlockers(_expandUnitsAnalyzer.FindExpandBlockers(expandLocation.Position));
        }

        var obstacleIds = new HashSet<uint>(Units.Obstacles.Concat(Units.MineralFields).Concat(Units.GasGeysers));
        obstacleIds.Remove(Units.UnbuildablePlatesDestructible); // It is destructible but you can walk on it
        foreach (var obstacle in Controller.GetUnits(_unitsTracker.NeutralUnits, obstacleIds)) {
            obstacle.AddDeathWatcher(this);
        }
    }

    public void Update(ResponseObservation observation, ResponseGameInfo gameInfo) {
        if (observation.Observation.GameLoop == 0) {
            Reset();

            var nbRegions = _regionsData.Regions.Count;
            var nbObstructed = _regionsData.Regions.Count(region => region.IsObstructed);
            var nbRamps = _regionsData.Ramps.Count;
            var nbNoise = _regionsData.Noise.Count;
            var nbChokePoints = _regionsData.ChokePoints.Count;
            Logger.Success("Regions loaded from file");
            Logger.Metric($"{nbRegions} regions ({nbObstructed} obstructed), {nbRamps} ramps, {nbNoise} unclassified cells and {nbChokePoints} choke points");
            Logger.Metric($"{_expandLocations.Count} expand locations");
        }

        // TODO GD Update obstructions

        Debug();
    }

    public IRegion GetRegion(Vector3 position) {
        return GetRegion(position.ToVector2());
    }

    public IRegion GetRegion(Vector2 position) {
        if (_regionsLookupMap.TryGetValue(position.AsWorldGridCenter(), out var region)) {
            return region;
        }

        if (_terrainTracker.IsWalkable(position) && !_regionsData.Noise.Contains(position)) {
            Logger.Warning($"Region not found for walkable position {position}");
        }

        return null;
    }

    public IExpandLocation GetExpand(Alliance alliance, ExpandType expandType) {
        var expands = _expandLocationsByType[expandType];

        return alliance == Alliance.Enemy
            ? expands.MinBy(expandLocation => expandLocation.Position.DistanceTo(_terrainTracker.EnemyStartingLocation))!
            : expands.MinBy(expandLocation => expandLocation.Position.DistanceTo(_terrainTracker.StartingLocation))!;
    }

    public IRegion GetNaturalExitRegion(Alliance alliance) {
        var natural = GetExpand(alliance, ExpandType.Natural);

        return _regionsData.Regions
            .Where(region => region.Type == RegionType.OpenArea)
            .MinBy(region => region.Center.DistanceTo(natural.Position))!;
    }

    // TODO GD Doesn't take into account the building dimensions, but good enough for creep spread since it's 1x1
    public bool IsNotBlockingExpand(Vector2 position) {
        // We could use Regions here, but I'd rather not because of dependencies
        var closestExpandLocation = _expandLocations
            .Select(expandLocation => expandLocation.Position)
            .MinBy(expandPosition => expandPosition.DistanceTo(position));

        return closestExpandLocation.DistanceTo(position) > ExpandRadius + 1;
    }

    /// <summary>
    /// Creates a cell to region lookup map for fast queries.
    /// </summary>
    /// <param name="regions">The regions to map</param>
    /// <returns>The cell to region lookup map</returns>
    private static Dictionary<Vector2, IRegion> BuildRegionsLookupMap(List<Region> regions) {
        var regionsMap = new Dictionary<Vector2, IRegion>();
        foreach (var region in regions) {
            foreach (var cell in region.Cells) {
                regionsMap[cell] = region;
            }
        }

        return regionsMap;
    }

    /// <summary>
    /// Enables graphical debugging of the RegionAnalyzer's data based on debug flags
    /// </summary>
    private void Debug() {
        if (_debuggingFlagsTracker.IsActive(DebuggingFlags.RegionCells)) {
            DrawRegions();
            DrawNoise();
        }

        if (_debuggingFlagsTracker.IsActive(DebuggingFlags.ChokePoints)) {
            DrawChokePoints();
        }
    }

    /// <summary>
    /// <para>Draws a square on each region's cells.</para>
    /// <para>Each region gets a different color using the color pool.</para>
    /// <para>Each cell also gets a text 'EX', where E stands for 'Expand' and X is the region index.</para>
    /// </summary>
    private void DrawRegions() {
        foreach (var region in _regionsData.Regions) {
            var frontier = region.Neighbors.SelectMany(neighboringRegion => neighboringRegion.Frontier).ToList();

            foreach (var position in region.Cells.Except(frontier)) {
                Program.GraphicalDebugger.AddText($"{region.Id}", size: 12, worldPos: _terrainTracker.WithWorldHeight(position).ToPoint(), color: region.Color);
                Program.GraphicalDebugger.AddGridSquare(_terrainTracker.WithWorldHeight(position), region.Color);
            }

            foreach (var position in frontier) {
                Program.GraphicalDebugger.AddText($"F{region.Id}", size: 12, worldPos: _terrainTracker.WithWorldHeight(position).ToPoint(), color: region.Color);
                Program.GraphicalDebugger.AddGridSphere(_terrainTracker.WithWorldHeight(position), region.Color);
            }
        }
    }

    /// <summary>
    /// <para>Draws a red square on each noise cell.</para>
    /// <para>A noise cell is a cell that isn't part of a region or ramp.</para>
    /// <para>Each cell also gets a text '?'.</para>
    /// </summary>
    private void DrawNoise() {
        foreach (var position in _regionsData.Noise) {
            Program.GraphicalDebugger.AddText("?", size: 12, worldPos: _terrainTracker.WithWorldHeight(position).ToPoint(), color: Colors.Red);
            Program.GraphicalDebugger.AddGridSphere(_terrainTracker.WithWorldHeight(position), Colors.Red);
        }
    }

    /// <summary>
    /// Draws all the choke points
    /// </summary>
    private void DrawChokePoints() {
        foreach (var chokePoint in _regionsData.ChokePoints) {
            Program.GraphicalDebugger.AddPath(chokePoint.Edge.Select(edge => _terrainTracker.WithWorldHeight(edge)).ToList(), Colors.LightRed, Colors.LightRed);
        }
    }

    /// <summary>
    /// Updates regions obstruction when an obstacle dies.
    /// We only death watch obstacles.
    /// </summary>
    /// <param name="deadUnit"></param>
    public void ReportUnitDeath(Unit deadUnit) {
        _regionsData.Regions
            .FirstOrDefault(region => region.Cells.Contains(deadUnit.Position.ToVector2()))
            ?.UpdateObstruction();
    }
}
