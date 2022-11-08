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

    private const int UpscalingFactor = 8;
    private static readonly HashSet<Color> RegionColors = new HashSet<Color>
    {
        Color.Aqua,
        Color.Red,
        Color.ForestGreen,
        Color.Blue,
        Color.DarkOrange,
        Color.MediumPurple
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

    private static void SaveAsImage(string mapName, List<Region> regions) {
        if (!OperatingSystem.IsWindows()) {
            return;
        }

        var regionsColors = new Dictionary<Region, Color>();
        foreach (var region in regions) {
            regionsColors[region] = Color.ForestGreen;
        }

        foreach (var region in regions) {
            var neighborColors = region.Neighbors.Select(neighbor => regionsColors[neighbor.Region]).ToHashSet();
            if (neighborColors.Contains(regionsColors[region])) {
                // There should be enough colors so that one is always available
                regionsColors[region] = RegionColors.Except(neighborColors).First();
            }
        }

        var image = new Bitmap(MapAnalyzer.MaxX, MapAnalyzer.MaxY);
        for (var x = 0; x < image.Width; x++) {
            for (var y = 0; y < image.Height; y++) {
                // This code needs to change if UpSamplingFactor factor changes, oh well lazy me
                image.SetPixel(x, y, Color.Black);
            }
        }

        foreach (var region in regions) {
            var pixelColor = regionsColors[region];
            foreach (var cell in region.Cells.Select(cell => cell.AsWorldGridCorner())) {
                // This code needs to change if UpSamplingFactor factor changes, oh well lazy me
                image.SetPixel((int)cell.X, (int)cell.Y, pixelColor);
            }
        }

        var imageSaveFilePath = GetFileName(mapName, "png");
        var upscaledImage = new Bitmap(image, new Size(image.Width * UpscalingFactor, image.Height * UpscalingFactor));
        upscaledImage.Save(imageSaveFilePath, ImageFormat.Png);
    }
}
