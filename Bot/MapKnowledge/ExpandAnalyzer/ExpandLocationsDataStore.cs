using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Bot.MapKnowledge;

// TODO GD We could most likely make this not static and a bit more generic
public static class ExpandLocationDataStore {
    public static bool IsEnabled = true;

    private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
    {
        Converters =
        {
            new JsonSerializers.Vector3JsonConverter(),
        }
    };

    private static string GetFileName(string mapName) {
        return $"Expands_{mapName.Replace(" ", "")}.json";
    }

    public static void Save(string mapName, List<ExpandLocation> expandLocations) {
        // Will output to bin/Debug/net6.0 or bin/Release/net6.0
        // Make sure to copy to the Data/ folder and set properties to 'Copy if newer'
        var saveFilePath = GetFileName(mapName);

        var jsonString = JsonSerializer.Serialize(expandLocations, JsonSerializerOptions);
        File.WriteAllText(saveFilePath, jsonString);
    }

    public static List<ExpandLocation> Load(string mapName) {
        if (!IsEnabled) {
            return null;
        }

        var loadFilePath = $"Data/{GetFileName(mapName)}";
        if (!File.Exists(loadFilePath)) {
            return null;
        }

        var jsonString = File.ReadAllText(loadFilePath);
        return JsonSerializer.Deserialize<List<ExpandLocation>>(jsonString, JsonSerializerOptions)!;
    }
}
