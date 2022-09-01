using System.IO;
using System.Text.Json;

namespace Bot.MapKnowledge;

public static class RegionDataStore {
    private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
    {
        Converters =
        {
            new JsonSerializers.Vector2JsonConverter(),
            new JsonSerializers.Vector3JsonConverter(),
        }
    };

    private static string GetFileName(string mapName) {
        return $"Regions_{mapName.Replace(" ", "")}.json";
    }

    public static void Save(string mapName, RegionData regionData) {
        // Will output to bin/Debug/net6.0 or bin/Release/net6.0
        // Make sure to copy to the Data/ folder and set properties to 'Copy if newer'
        var saveFilePath = GetFileName(mapName);

        var jsonString = JsonSerializer.Serialize(regionData, JsonSerializerOptions);
        File.WriteAllText(saveFilePath, jsonString);
    }

    public static RegionData Load(string mapName) {
        var loadFilePath = $"Data/{GetFileName(mapName)}";
        if (!File.Exists(loadFilePath)) {
            return null;
        }

        var jsonString = File.ReadAllText(loadFilePath);
        return JsonSerializer.Deserialize<RegionData>(jsonString, JsonSerializerOptions)!;
    }
}
