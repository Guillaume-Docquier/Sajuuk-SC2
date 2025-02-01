namespace MapAnalysis.ExpandAnalysis;

/// <summary>
/// An analyzer that finds the expand locations in the map.
/// </summary>
public interface IExpandAnalyzer : IAnalyzer {
    /// <summary>
    /// The expand locations found by this analyzer.
    /// The list will likely be empty until the analysis is done.
    /// </summary>
    public IEnumerable<IExpandLocation> ExpandLocations { get; }
}
