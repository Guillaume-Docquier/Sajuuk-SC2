namespace MapAnalysis.RegionAnalysis.ChokePoints;

public interface IChokeFinder {
    /// <summary>
    /// Finds choke points in the current map.
    /// TODO GD This should take a list of cells to find choke points from
    /// </summary>
    /// <returns>The list of potential choke points.</returns>
    List<ChokePoint> FindChokePoints();
}
