using System.Numerics;

namespace MapAnalysis.RegionAnalysis;

// TODO GD Change this to be a "frontier" instead and add if the frontier is blocked
// TODO GD Add fromRegion and toRegion to determine which regions this frontier is for)
// TODO GD Might be a set or regions instead, we'll see (could be more than 2 bordering regions? Or we have multiple frontiers for the same border? Meh)
public interface INeighboringRegion {
    public IRegion Region { get; }
    public HashSet<Vector2> Frontier { get; }
}
