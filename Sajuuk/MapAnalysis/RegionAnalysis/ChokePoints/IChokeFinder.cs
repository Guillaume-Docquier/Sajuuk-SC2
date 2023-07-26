using System.Collections.Generic;

namespace Sajuuk.MapAnalysis.RegionAnalysis.ChokePoints;

public interface IChokeFinder {
    /// <summary>
    /// Finds choke points in the current map.
    /// </summary>
    /// <returns>The list of potential choke points.</returns>
    List<ChokePoint> FindChokePoints();
}
