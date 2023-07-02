using System.Collections.Generic;
using System.Numerics;
using Sajuuk.MapAnalysis.ExpandAnalysis;
using Sajuuk.MapAnalysis.RegionAnalysis;
using SC2APIProtocol;

namespace Sajuuk.GameSense;

public interface IRegionsTracker {
    public IEnumerable<IRegion> Regions { get; }
    public IEnumerable<IExpandLocation> ExpandLocations { get; }

    public bool IsBlockingExpand(Vector2 position);
    public IRegion GetRegion(Vector2 position);
    public IRegion GetRegion(Vector3 position);

    /// <summary>
    /// Gets an expand location of yourself or the enemy
    /// </summary>
    /// <param name="alliance">Yourself or the enemy</param>
    /// <param name="expandType">The expand type</param>
    /// <returns>The requested expand location</returns>
    public IExpandLocation GetExpand(Alliance alliance, ExpandType expandType);

    public IRegion GetNaturalExitRegion(Alliance alliance);
}
