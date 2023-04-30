using System.Collections.Generic;
using System.Numerics;

namespace Bot.MapAnalysis.ExpandAnalysis;

public interface IExpandUnitsAnalyzer {
    /// <summary>
    /// Finds all the resource clusters in the map that could be associated with an expand.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<List<Unit>> FindResourceClusters();

    /// <summary>
    /// Returns the resource cluster that is the most likely associated with the provided expand position.
    /// </summary>
    /// <param name="expandPosition"></param>
    /// <returns>The resource cluster of that expand position</returns>
    public HashSet<Unit> FindExpandResources(Vector2 expandPosition);

    /// <summary>
    /// Finds all units that need to be cleared to take the expand, typically mineral fields or rocks
    /// </summary>
    /// <param name="expandLocation"></param>
    /// <returns>All units that need to be cleared to take the expand</returns>
    public HashSet<Unit> FindExpandBlockers(Vector2 expandLocation);
}
