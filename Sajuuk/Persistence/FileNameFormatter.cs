namespace Sajuuk.Persistence;

public static class FileNameFormatter {
    public static string FormatDataFileName(string id, string mapFileName, string extension) {
        return $"Data/{id}_{mapFileName.Replace(".SC2Map", "").Replace(" ", "").ToLower()}.{extension}";
    }
}
