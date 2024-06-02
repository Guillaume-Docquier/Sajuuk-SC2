using System.Numerics;

namespace MapAnalysis.RegionAnalysis;

public class RegionFactory : IRegionFactory {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IClustering _clustering;
    private readonly IPathfinder _pathfinder;
    private readonly IUnitsTracker _unitsTracker;

    public RegionFactory(
        ITerrainTracker terrainTracker,
        IClustering clustering,
        IPathfinder pathfinder,
        IUnitsTracker unitsTracker
    ) {
        _terrainTracker = terrainTracker;
        _clustering = clustering;
        _pathfinder = pathfinder;
        _unitsTracker = unitsTracker;
    }

    public AnalyzedRegion CreateAnalyzedRegion(IEnumerable<Vector2> cells, RegionType regionType, IEnumerable<ExpandLocation> expandLocations) {
        return new AnalyzedRegion(_terrainTracker, _clustering, _pathfinder, _unitsTracker, cells, regionType, expandLocations);
    }
}
