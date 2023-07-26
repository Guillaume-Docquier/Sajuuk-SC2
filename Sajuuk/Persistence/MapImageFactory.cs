using Sajuuk.GameSense;

namespace Sajuuk.Persistence;

public class MapImageFactory : IMapImageFactory {
    private readonly ITerrainTracker _terrainTracker;

    public MapImageFactory(ITerrainTracker terrainTracker) {
        _terrainTracker = terrainTracker;
    }

    public IMapImage CreateMapImage() {
        return new MapImage(_terrainTracker);
    }
}
