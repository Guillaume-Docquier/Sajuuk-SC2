﻿using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sajuuk.ExtensionMethods;
using Sajuuk.Debugging;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.GameData;
using Sajuuk.MapAnalysis.ExpandAnalysis;
using Sajuuk.MapAnalysis.RegionAnalysis;
using Sajuuk.Persistence;
using SC2APIProtocol;

namespace Sajuuk.GameSense;

public class RegionsTracker : IRegionsTracker, INeedUpdating, IWatchUnitsDie {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IDebuggingFlagsTracker _debuggingFlagsTracker;
    private readonly IMapDataRepository<RegionsData> _regionsDataRepository;
    private readonly IExpandUnitsAnalyzer _expandUnitsAnalyzer;
    private readonly IGraphicalDebugger _graphicalDebugger;
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
        IUnitsTracker unitsTracker,
        IMapDataRepository<RegionsData> regionsDataRepository,
        IExpandUnitsAnalyzer expandUnitsAnalyzer,
        IGraphicalDebugger graphicalDebugger
    ) {
        _terrainTracker = terrainTracker;
        _debuggingFlagsTracker = debuggingFlagsTracker;
        _unitsTracker = unitsTracker;
        _regionsDataRepository = regionsDataRepository;
        _expandUnitsAnalyzer = expandUnitsAnalyzer;
        _graphicalDebugger = graphicalDebugger;
    }

    private void Init(string mapName) {
        _regionsData = _regionsDataRepository.Load(mapName);

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
        foreach (var obstacle in _unitsTracker.GetUnits(_unitsTracker.NeutralUnits, obstacleIds)) {
            obstacle.AddDeathWatcher(this);
        }
    }

    public void Update(ResponseObservation observation, ResponseGameInfo gameInfo) {
        if (observation.Observation.GameLoop == 0) {
            Init(gameInfo.MapName);

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
    public bool IsBlockingExpand(Vector2 position) {
        var region = GetRegion(position);
        if (region?.ExpandLocation == null) {
            return false;
        }

        return region.ExpandLocation.Position.DistanceTo(position) <= ExpandRadius + 1;
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
                _graphicalDebugger.AddText($"{region.Id}", size: 12, worldPos: _terrainTracker.WithWorldHeight(position).ToPoint(), color: region.Color);
                _graphicalDebugger.AddGridSquare(_terrainTracker.WithWorldHeight(position), region.Color);
            }

            foreach (var position in frontier) {
                _graphicalDebugger.AddText($"F{region.Id}", size: 12, worldPos: _terrainTracker.WithWorldHeight(position).ToPoint(), color: region.Color);
                _graphicalDebugger.AddGridSphere(_terrainTracker.WithWorldHeight(position), region.Color);
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
            _graphicalDebugger.AddText("?", size: 12, worldPos: _terrainTracker.WithWorldHeight(position).ToPoint(), color: Colors.Red);
            _graphicalDebugger.AddGridSphere(_terrainTracker.WithWorldHeight(position), Colors.Red);
        }
    }

    /// <summary>
    /// Draws all the choke points
    /// </summary>
    private void DrawChokePoints() {
        foreach (var chokePoint in _regionsData.ChokePoints) {
            _graphicalDebugger.AddPath(chokePoint.Edge.Select(edge => _terrainTracker.WithWorldHeight(edge)).ToList(), Colors.LightRed, Colors.LightRed);
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
