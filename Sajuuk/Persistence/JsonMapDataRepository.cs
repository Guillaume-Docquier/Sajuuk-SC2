using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sajuuk.Persistence;

public class JsonMapDataRepository<TData> : IMapDataRepository<TData> {
    private readonly Func<string, string> _getFileName;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        ReferenceHandler = ReferenceHandler.Preserve,
        MaxDepth = 256,
        Converters =
        {
            new JsonSerializers.Vector2JsonConverter(),
            new JsonSerializers.Vector3JsonConverter(),
        },
    };

    public JsonMapDataRepository(Func<string, string> getFileName) {
        _getFileName = getFileName;
    }

    public void Save(TData data, string mapFileName) {
        var jsonString = JsonSerializer.Serialize(data, _jsonSerializerOptions);

        // Make sure to copy and set properties to 'Copy if newer'
        // TODO GD Print it?

        var fileName = _getFileName(mapFileName);
        CreateDirectoryIfNotExists(fileName);
        File.WriteAllText(fileName, jsonString);
        Logger.Success($"JSON data saved to {fileName}");
    }

    public TData Load(string mapFileName) {
        var fileName = _getFileName(mapFileName);
        if (!File.Exists(fileName)) {
            return default;
        }

        var jsonString = File.ReadAllText(fileName);
        return JsonSerializer.Deserialize<TData>(jsonString, _jsonSerializerOptions)!;
    }

    private static void CreateDirectoryIfNotExists(string filePath) {
        var directory = Path.GetDirectoryName(filePath);
        if (directory != null) {
            Directory.CreateDirectory(directory);
        }
    }
}
