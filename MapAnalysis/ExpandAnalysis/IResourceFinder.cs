using System.Numerics;
using SC2Client.State;

namespace MapAnalysis.ExpandAnalysis;

public interface IResourceFinder {
    /// <summary>
    /// Finds all the resource clusters in the map that could be associated with an expand.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<List<IUnit>> FindResourceClusters();

    /// <summary>
    /// Returns the resource cluster that is the most likely associated with the provided expand position.
    /// </summary>
    /// <param name="expandPosition"></param>
    /// <returns>The resource cluster of that expand position</returns>
    public HashSet<IUnit> FindExpandResources(Vector2 expandPosition);

    /// <summary>
    /// Finds all units that need to be cleared to take the expand, typically mineral fields or rocks
    /// </summary>
    /// <param name="expandLocation"></param>
    /// <returns>All units that need to be cleared to take the expand</returns>
    public HashSet<IUnit> FindExpandBlockers(Vector2 expandLocation);
}
