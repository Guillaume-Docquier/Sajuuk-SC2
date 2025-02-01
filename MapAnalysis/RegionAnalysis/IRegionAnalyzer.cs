namespace MapAnalysis.RegionAnalysis;

public interface IRegionAnalyzer : IAnalyzer {
    public List<IRegion> Regions { get; }
}
