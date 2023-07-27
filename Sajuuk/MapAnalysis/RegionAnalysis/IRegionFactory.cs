using System.Collections.Generic;
using System.Numerics;
using Sajuuk.MapAnalysis.ExpandAnalysis;

namespace Sajuuk.MapAnalysis.RegionAnalysis;

public interface IRegionFactory {
    AnalyzedRegion CreateAnalyzedRegion(IEnumerable<Vector2> cells, RegionType regionType, IEnumerable<ExpandLocation> expandLocations);
}