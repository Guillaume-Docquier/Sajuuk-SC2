using System.Collections.Generic;
using System.Drawing;
using Sajuuk.Algorithms;
using Sajuuk.Debugging.GraphicalDebugging;
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

    // The colors match those used in AnalyzedRegion
    private static readonly Dictionary<SC2APIProtocol.Color, Color> RegionColorsMapping = new Dictionary<SC2APIProtocol.Color, Color>
    {
        { Colors.Cyan, Color.Teal},
        { Colors.Magenta, Color.Purple},
        { Colors.Orange, Color.Olive},
        { Colors.Blue, Color.MediumBlue},
        { Colors.Red, Color.Maroon},
        { Colors.LimeGreen, Color.Green},
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
        var mapImage = _mapImageFactory.CreateMapImage();
        foreach (var region in regions) {
            foreach (var cell in region.Cells) {
                mapImage.SetCellColor(cell, RegionColorsMapping[region.Color]);
            }

            if (region.ExpandLocation != null) {
                PaintExpandLocation(mapImage, region.ExpandLocation);
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
