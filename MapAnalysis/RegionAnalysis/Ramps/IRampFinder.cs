using System.Numerics;

namespace MapAnalysis.RegionAnalysis.Ramps;

public interface IRampFinder {
    /// <summary>
    /// Identify ramps within the given cells.
    /// </summary>
    /// <param name="cellsToConsider">The cells to use to find ramps.</param>
    /// <returns>The ramps that were found.</returns>
    public List<Ramp> FindRamps(IEnumerable<Vector2> cellsToConsider);
}
