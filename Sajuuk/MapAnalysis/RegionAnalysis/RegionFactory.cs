using System.Collections.Generic;
using System.Numerics;
using Sajuuk.Algorithms;
using Sajuuk.GameSense;
using Sajuuk.MapAnalysis.ExpandAnalysis;

namespace Sajuuk.MapAnalysis.RegionAnalysis;

public class RegionFactory : IRegionFactory {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IClustering _clustering;
    private readonly IPathfinder _pathfinder;

    public RegionFactory(
        ITerrainTracker terrainTracker,
        IClustering clustering,
        IPathfinder pathfinder
    ) {
        _terrainTracker = terrainTracker;
        _clustering = clustering;
        _pathfinder = pathfinder;
    }

    public AnalyzedRegion CreateAnalyzedRegion(IEnumerable<Vector2> cells, RegionType regionType, IEnumerable<ExpandLocation> expandLocations) {
        return new AnalyzedRegion(_terrainTracker, _clustering, _pathfinder, cells, regionType, expandLocations);
    }
}
