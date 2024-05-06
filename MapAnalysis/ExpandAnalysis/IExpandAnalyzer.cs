namespace MapAnalysis.ExpandAnalysis;

public interface IExpandAnalyzer {
    public bool IsAnalysisComplete { get; }
    public IEnumerable<ExpandLocation> ExpandLocations { get; } // We need the concrete type in RegionAnalyzer
}
