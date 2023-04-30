using System.Collections.Generic;

namespace Bot.MapAnalysis.RegionAnalysis;

public interface IRegionAnalyzer {
    public bool IsAnalysisComplete { get; }
    public List<IRegion> Regions { get; }
}
