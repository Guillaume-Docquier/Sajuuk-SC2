using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.Persistence;

namespace Bot.MapAnalysis.RegionAnalysis;

public class RegionsDataRepository : IMapDataRepository<RegionsData> {
    private readonly ITerrainTracker _terrainTracker;

    private readonly JsonMapDataRepository<RegionsData> _jsonMapDataRepository;

    private readonly string _fileName;

    private const int UpscalingFactor = 4;
    private static readonly HashSet<Color> RegionColors = new HashSet<Color>
    {
        Color.Cyan,
        Color.Red,
        Color.Lime,
        Color.Blue,
        Color.Orange,
        Color.Magenta
    };

    // TODO GD Can we know the map name before starting the game on the ladder?
    public RegionsDataRepository(ITerrainTracker terrainTracker, string mapFileName) {
        _terrainTracker = terrainTracker;

        _fileName = $"Data/Regions_{mapFileName.Replace(".SC2Map", "").Replace(" ", "").ToLower()}";
        _jsonMapDataRepository = new JsonMapDataRepository<RegionsData>($"{_fileName}.json");
    }

    /// <summary>
    /// Saves the regions data as JSON, and saves a PNG image of the saved regions.
    /// </summary>
    /// <param name="data">The regions data to save.</param>
    public void Save(RegionsData data) {
        _jsonMapDataRepository.Save(data);
        SaveAsImage(data.Regions);
    }

    /// <summary>
    /// Loads the regions data.
    /// </summary>
    /// <returns>The loaded regions data.</returns>
    public RegionsData Load() {
        return _jsonMapDataRepository.Load();
    }

    /// <summary>
    /// Saves the regions as an image where each region has a different color than its neighbors.
    /// The saved imaged is actually upside down because the SC2 origin is the bottom left, while an image origin is top left.
    /// (Oh well. Fix it if you want, I don't)
    /// </summary>
    /// <param name="regions"></param>
    private void SaveAsImage(List<Region> regions) {
        if (!OperatingSystem.IsWindows()) {
            return;
        }

        var regionsColors = new Dictionary<IRegion, Color>();
        foreach (var region in regions) {
            regionsColors[region] = Color.Cyan;
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
        }

        ScaleImage(image, UpscalingFactor).Save($"{_fileName}.png", ImageFormat.Png);
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
}
