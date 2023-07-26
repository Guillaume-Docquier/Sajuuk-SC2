using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Sajuuk.Algorithms;
using Sajuuk.GameSense;
using Sajuuk.MapAnalysis.ExpandAnalysis;
using Sajuuk.Persistence;

namespace Sajuuk.MapAnalysis.RegionAnalysis;

public class RegionsDataRepository : IMapDataRepository<RegionsData> {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IClustering _clustering;
    private readonly IPathfinder _pathfinder;
    private readonly FootprintCalculator _footprintCalculator;
    private readonly IMapImageFactory _mapImageFactory;

    private readonly JsonMapDataRepository<RegionsData> _jsonMapDataRepository;

    private const string FileNameId = "Regions";

    private static readonly Color MineralColor = Color.Cyan;
    private static readonly Color GasColor = Color.Lime;
    private static readonly Color ExpandColor = Color.Magenta;
    private static readonly HashSet<Color> RegionColors = new HashSet<Color>
    {
        Color.Teal,
        Color.Green,
        Color.Purple,
        Color.Navy,
        Color.Olive,
        Color.Maroon,
    };

    public RegionsDataRepository(
        ITerrainTracker terrainTracker,
        IClustering clustering,
        IPathfinder pathfinder,
        FootprintCalculator footprintCalculator,
        IMapImageFactory mapImageFactory
    ) {
        _terrainTracker = terrainTracker;
        _clustering = clustering;
        _pathfinder = pathfinder;
        _footprintCalculator = footprintCalculator;
        _mapImageFactory = mapImageFactory;

        _jsonMapDataRepository = new JsonMapDataRepository<RegionsData>(mapFileName => FileNameFormatter.FormatDataFileName(FileNameId, mapFileName, "json"));
    }

    /// <summary>
    /// Saves the regions data as JSON, and saves a PNG image of the saved regions.
    /// </summary>
    /// <param name="regionsData">The regions data to save.</param>
    /// <param name="mapFileName"></param>
    public void Save(RegionsData regionsData, string mapFileName) {
        _jsonMapDataRepository.Save(regionsData, mapFileName);
        SaveAsImage(regionsData.Regions, mapFileName);
    }

    /// <summary>
    /// Loads the regions data.
    /// </summary>
    /// <returns>The loaded regions data.</returns>
    public RegionsData Load(string mapFileName) {
        var regionsData = _jsonMapDataRepository.Load(mapFileName);

        regionsData.Regions.ForEach(region => region.SetDependencies(_terrainTracker, _clustering, _pathfinder));

        return regionsData;
    }

    /// <summary>
    /// Saves the regions as an image where each region has a different color than its neighbors.
    /// </summary>
    /// <param name="regions">The regions to represent.</param>
    /// <param name="mapFileName">The file name of the current map.</param>
    private void SaveAsImage(List<Region> regions, string mapFileName) {
        var regionsColors = new Dictionary<IRegion, Color>();
        var baseColor = RegionColors.First();
        foreach (var region in regions) {
            regionsColors[region] = baseColor;
        }

        var rng = new Random();
        foreach (var region in regions) {
            var neighborColors = region.Neighbors.Select(neighbor => regionsColors[neighbor.Region]).ToHashSet();
            if (neighborColors.Contains(regionsColors[region])) {
                // There should be enough colors so that one is always available
                var availableColors = RegionColors.Except(neighborColors).ToList();

                var randomColorIndex = rng.Next(availableColors.Count);
                regionsColors[region] = availableColors[randomColorIndex];
            }
        }

        var mapImage = _mapImageFactory.CreateMapImage();
        foreach (var region in regions) {
            var regionColor = regionsColors[region];
            foreach (var cell in region.Cells) {
                mapImage.SetCellColor(cell, regionColor);
            }

            if (region.ConcreteExpandLocation != null) {
                PaintExpandLocation(mapImage, region.ConcreteExpandLocation);
            }
        }

        mapImage.Save(FileNameFormatter.FormatDataFileName(FileNameId, mapFileName, "png"));
    }

    /// <summary>
    /// Paints the expand location and its resources on the map image.
    /// </summary>
    /// <param name="mapImage">The map image to paint on.</param>
    /// <param name="expandLocation">The expand location data to paint.</param>
    private void PaintExpandLocation(IMapImage mapImage, IExpandLocation expandLocation) {
        mapImage.SetCellColor(expandLocation.Position, ExpandColor);

        foreach (var resource in expandLocation.Resources) {
            var resourceColor = Resources.GetResourceType(resource) switch
            {
                Resources.ResourceType.Mineral => MineralColor,
                Resources.ResourceType.Gas => GasColor,
                _ => Color.Black
            };

            foreach (var resourceCell in _footprintCalculator.GetFootprint(resource)) {
                mapImage.SetCellColor(resourceCell, resourceColor);
            }
        }
    }
}
