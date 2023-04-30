using Bot.Persistence;

namespace Bot.MapAnalysis.RegionAnalysis;

public class RegionsDataRepository : JsonMapDataRepository<RegionsData> {
    // TODO GD Can we know the map name before starting the game on the ladder?
    public RegionsDataRepository(string mapFileName)
        : base($"Data/Regions_{mapFileName.Replace(".SC2Map", "").Replace(" ", "").ToLower()}.json") {}
}
