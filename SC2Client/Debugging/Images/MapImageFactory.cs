using System.Drawing;
using SC2Client.Trackers;

namespace SC2Client.Debugging.Images;

public class MapImageFactory : IMapImageFactory {
    private readonly ILogger _logger;
    private readonly ITerrainTracker _terrainTracker;

    public MapImageFactory(ILogger logger, ITerrainTracker terrainTracker) {
        _logger = logger;
        _terrainTracker = terrainTracker;
    }

    /// <summary>
    /// Creates a map image with the playable terrain in white.
    /// </summary>
    /// <returns></returns>
    public IMapImage CreateMapImageWithTerrain() {
        var mapImage = new MapImage(_logger, _terrainTracker.MaxX, _terrainTracker.MaxY);
        mapImage.SetCellsColor(_terrainTracker.Cells, Color.White);

        return mapImage;
    }
}
