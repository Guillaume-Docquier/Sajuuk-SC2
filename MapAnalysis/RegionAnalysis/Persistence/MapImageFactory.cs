using SC2Client;
using SC2Client.Trackers;

namespace MapAnalysis.RegionAnalysis.Persistence;

public class MapImageFactory : IMapImageFactory {
    private readonly ILogger _logger;
    private readonly ITerrainTracker _terrainTracker;

    public MapImageFactory(ILogger logger, ITerrainTracker terrainTracker) {
        _logger = logger;
        _terrainTracker = terrainTracker;
    }

    public IMapImage CreateMapImage() {
        return new MapImage(_logger, _terrainTracker);
    }
}
