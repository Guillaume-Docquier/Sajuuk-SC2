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

    private static string GetSaveFilePath(string mapName) {
        return $"Regions_{mapName.Replace(" ", "")}.json";
    }

    public static void Save(string mapName, RegionData regionData) {
        var saveFilePath = GetSaveFilePath(mapName);

        var jsonString = JsonSerializer.Serialize(regionData, JsonSerializerOptions);
        File.WriteAllText(saveFilePath, jsonString);
    }

    public static RegionData Load(string mapName) {
        var saveFilePath = GetSaveFilePath(mapName);
        if (!File.Exists(saveFilePath)) {
            return null;
        }

        var jsonString = File.ReadAllText(saveFilePath);
        return JsonSerializer.Deserialize<RegionData>(jsonString, JsonSerializerOptions)!;
    }
}
