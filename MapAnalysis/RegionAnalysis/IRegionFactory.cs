using System.Numerics;

namespace MapAnalysis.RegionAnalysis;

public interface IRegionFactory {
    AnalyzedRegion CreateAnalyzedRegion(IEnumerable<Vector2> cells, RegionType regionType, IEnumerable<ExpandLocation> expandLocations);
}