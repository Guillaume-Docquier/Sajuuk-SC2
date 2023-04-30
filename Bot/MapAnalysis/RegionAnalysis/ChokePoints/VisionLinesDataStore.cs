using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Bot.MapAnalysis.RegionAnalysis.ChokePoints;

public partial class RayCastingChokeFinder {
    public static class VisionLinesDataStore {
        public static bool IsEnabled = false;

        private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
        {
            Converters =
            {
                new JsonSerializers.Vector2JsonConverter(),
                new JsonSerializers.Vector3JsonConverter(),
            },
        };

        private static string GetFileName(string mapName) {
            return $"VisionLines_{mapName.Replace(" ", "")}.json";
        }

        public static void Save(string mapName, List<VisionLine> visionLines) {
            // Will output to bin/Debug/net6.0 or bin/Release/net6.0
            // Make sure to copy to the Data/ folder and set properties to 'Copy if newer'
            var saveFilePath = GetFileName(mapName);

            var jsonString = JsonSerializer.Serialize(visionLines, JsonSerializerOptions);
            File.WriteAllText(saveFilePath, jsonString);
        }

        public static List<VisionLine> Load(string mapName) {
            if (!IsEnabled) {
                return null;
            }

            var loadFilePath = $"Data/{GetFileName(mapName)}";
            if (!File.Exists(loadFilePath)) {
                return null;
            }

            var jsonString = File.ReadAllText(loadFilePath);
            return JsonSerializer.Deserialize<List<VisionLine>>(jsonString, JsonSerializerOptions)!;
        }
    }
}
