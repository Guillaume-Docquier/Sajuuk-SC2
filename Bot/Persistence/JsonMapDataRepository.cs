﻿using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bot.Persistence;

public class JsonMapDataRepository<TData> : IMapDataRepository<TData> {
    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        ReferenceHandler = ReferenceHandler.Preserve,
        Converters =
        {
            new JsonSerializers.Vector2JsonConverter(),
            new JsonSerializers.Vector3JsonConverter(),
        }
    };

    private readonly string _fileName;

    public JsonMapDataRepository(string fileName) {
        _fileName = fileName;
    }

    public void Save(TData regions) {
        var jsonString = JsonSerializer.Serialize(regions, _jsonSerializerOptions);

        // Make sure to copy and set properties to 'Copy if newer'
        // TODO GD Print it?

        CreateDirectoryIfNotExists(_fileName);
        File.WriteAllText(_fileName, jsonString);
    }

    public TData Load() {
        if (!File.Exists(_fileName)) {
            return default;
        }

        var jsonString = File.ReadAllText(_fileName);
        return JsonSerializer.Deserialize<TData>(jsonString, _jsonSerializerOptions)!;
    }

    private static void CreateDirectoryIfNotExists(string filePath) {
        var directory = Path.GetDirectoryName(filePath);
        if (directory != null) {
            Directory.CreateDirectory(directory);
        }
    }
}