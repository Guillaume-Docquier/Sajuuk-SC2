using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Bot.ExtensionMethods;

namespace Bot.MapKnowledge;

public static class RegionDataStore {
    public static bool IsEnabled = true;

    private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
    {
        Converters =
        {
            new JsonSerializers.Vector2JsonConverter(),
            new JsonSerializers.Vector3JsonConverter(),
        }
    };

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

    private static string GetFileName(string mapName, string format) {
        return $"Regions_{mapName.Replace(" ", "")}.{format}";
    }

    public static void Save(string mapName, RegionData regionData) {
        // Will output to bin/Debug/net6.0 or bin/Release/net6.0
        // Make sure to copy to the Data/ folder and set properties to 'Copy if newer'
        var jsonSaveFilePath = GetFileName(mapName, "json");

        var jsonString = JsonSerializer.Serialize(regionData, JsonSerializerOptions);
        File.WriteAllText(jsonSaveFilePath, jsonString);

        SaveAsImage(mapName, regionData.Regions);
    }

    public static RegionData Load(string mapName) {
        if (!IsEnabled) {
            return null;
        }

        var loadFilePath = $"Data/{GetFileName(mapName, "json")}";
        if (!File.Exists(loadFilePath)) {
            return null;
        }

        var jsonString = File.ReadAllText(loadFilePath);
        return JsonSerializer.Deserialize<RegionData>(jsonString, JsonSerializerOptions)!;
    }

    /// <summary>
    /// Saves the regions as an image where each region has a different color than its neighbors.
    /// The saved imaged is actually upside down because the SC2 origin is the bottom left, while an image origin is top left.
    /// (Oh well. Fix it if you want, I don't)
    /// </summary>
    /// <param name="mapName"></param>
    /// <param name="regions"></param>
    private static void SaveAsImage(string mapName, List<Region> regions) {
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

        var image = new Bitmap(MapAnalyzer.MaxX, MapAnalyzer.MaxY);
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

        var imageSaveFilePath = GetFileName(mapName, "png");
        ScaleImage(image, UpscalingFactor).Save(imageSaveFilePath, ImageFormat.Png);
    }

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
