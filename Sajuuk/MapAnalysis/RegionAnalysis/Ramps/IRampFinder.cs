using System.Collections.Generic;
using System.Numerics;

namespace Sajuuk.MapAnalysis.RegionAnalysis.Ramps;

public interface IRampFinder {
    /// <summary>
    /// Identify ramps given cells that are walkable but not buildable.
    /// Some noise will be produced because some unbuildable cells are vision blockers and they should be used to find regions.
    /// </summary>
    /// <returns>
    /// The ramps and the cells that are not part of any ramp.
    /// </returns>
    (List<HashSet<Vector2>> ramps, IEnumerable<MapCell> rampsNoise) FindRamps(IEnumerable<MapCell> walkableCells);
}