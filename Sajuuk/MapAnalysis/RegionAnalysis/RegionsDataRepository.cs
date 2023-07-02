using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Sajuuk.ExtensionMethods;
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

    private readonly JsonMapDataRepository<RegionsData> _jsonMapDataRepository;

    private const int UpscalingFactor = 4;

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
        FootprintCalculator footprintCalculator
    ) {
        _terrainTracker = terrainTracker;
        _clustering = clustering;
        _pathfinder = pathfinder;
        _footprintCalculator = footprintCalculator;

        _jsonMapDataRepository = new JsonMapDataRepository<RegionsData>(mapFileName => GetFileName(mapFileName, "json"));
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
    /// <param name="regions"></param>
    /// <param name="mapFileName"></param>
    private void SaveAsImage(List<Region> regions, string mapFileName) {
        if (!OperatingSystem.IsWindows()) {
            return;
        }

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

        var image = new Bitmap(_terrainTracker.MaxX, _terrainTracker.MaxY);
        for (var x = 0; x < image.Width; x++) {
            for (var y = 0; y < image.Height; y++) {
                image.SetPixel(x, y, Color.Black);
            }
        }

        foreach (var region in regions) {
            var pixelColor = regionsColors[region];
            foreach (var cell in region.Cells.Select(cell => cell.AsWorldGridCorner())) {
                image.SetPixel((int)cell.X, (int)cell.Y, pixelColor);
            }

            if (region.ConcreteExpandLocation != null) {
                PaintExpandLocation(image, region.ConcreteExpandLocation);
            }
        }

        var scaledImage = ScaleImage(image, UpscalingFactor);
        scaledImage.RotateFlip(RotateFlipType.RotateNoneFlipY);
        scaledImage.Save(GetFileName(mapFileName, "png"));
    }

    /// <summary>
    /// Paints the expand location and its resources on the image.
    /// </summary>
    /// <param name="image">The image to paint on.</param>
    /// <param name="expandLocation">The expand location data to paint.</param>
    private void PaintExpandLocation(Bitmap image, IExpandLocation expandLocation) {
        if (!OperatingSystem.IsWindows()) {
            return;
        }

        image.SetPixel((int)expandLocation.Position.X, (int)expandLocation.Position.Y, ExpandColor);

        foreach (var resource in expandLocation.Resources) {
            var resourceColor = Resources.GetResourceType(resource) switch
            {
                Resources.ResourceType.Mineral => MineralColor,
                Resources.ResourceType.Gas => GasColor,
                _ => Color.Black
            };

            foreach (var cell in _footprintCalculator.GetFootprint(resource)) {
                image.SetPixel((int)cell.X, (int)cell.Y, resourceColor);
            }
        }
    }

    /// <summary>
    /// Scales the image so that it is bigger.
    /// This method only works on windows.
    /// </summary>
    /// <param name="originalImage">The original image.</param>
    /// <param name="scalingFactor">A scaling multiplier to indicate how much to scale the image.</param>
    /// <returns>The new, scaled, image.</returns>
    private static Bitmap ScaleImage(Bitmap originalImage, int scalingFactor) {
        if (!OperatingSystem.IsWindows()) {
            return originalImage;
        }

        var scaledWidth = originalImage.Width * scalingFactor;
        var scaledHeight = originalImage.Height * scalingFactor;
        var scaledImage = new Bitmap(scaledWidth, scaledHeight);

        for (var x = 0; x < originalImage.Width; x++) {
            for (var y = 0; y < originalImage.Height; y++) {
                for (var virtualX = 0; virtualX < scalingFactor; virtualX++) {
                    for (var virtualY = 0; virtualY < scalingFactor; virtualY++) {
                        scaledImage.SetPixel(
                            x * scalingFactor + virtualX,
                            y * scalingFactor + virtualY,
                            originalImage.GetPixel(x, y)
                        );
                    }
                }
            }
        }

        return scaledImage;
    }

    private static string GetFileName(string mapFileName, string extension) {
        return $"Data/Regions_{mapFileName.Replace(".SC2Map", "").Replace(" ", "").ToLower()}.{extension}";
    }
}
