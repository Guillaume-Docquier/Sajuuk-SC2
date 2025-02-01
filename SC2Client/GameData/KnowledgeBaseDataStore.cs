using Google.Protobuf;
using SC2APIProtocol;
using CodedOutputStream = Google.Protobuf.CodedOutputStream;

namespace SC2Client.GameData;

// TODO GD No static
public static class KnowledgeBaseDataStore {
    private static string GetFileName() {
        return "KnowledgeBaseData.proto";
    }

    public static void Save(ResponseData responseData) {
        // Will output to bin/Debug/net6.0 or bin/Release/net6.0
        // Make sure to copy to the Data/ folder and set properties to 'Copy if newer'
        var saveFilePath = GetFileName();
        if (File.Exists(saveFilePath)) {
            File.Delete(saveFilePath);
        }

        using var fileStream = File.OpenWrite(saveFilePath!);
        var codedOutputStream = new CodedOutputStream(fileStream);
        responseData.WriteTo(codedOutputStream);
    }

    public static ResponseData? Load() {
        var loadFilePath = $"Data/{GetFileName()}";
        if (!File.Exists(loadFilePath)) {
            return null;
        }

        using var fileStream = File.OpenRead(loadFilePath);
        var codedInputStream = new CodedInputStream(fileStream);

        var responseData = new ResponseData();
        responseData.MergeFrom(codedInputStream);

        return responseData;
    }
}
