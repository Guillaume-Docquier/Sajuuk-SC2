using System.Collections.Generic;
using System.Numerics;
using Bot.MapAnalysis.ExpandAnalysis;
using Bot.MapAnalysis.RegionAnalysis;
using SC2APIProtocol;

namespace Bot.GameSense;

public interface IRegionsTracker {
    public IEnumerable<IRegion> Regions { get; }

    // TODO GD We might not need to expose expand locations?
    public IEnumerable<IExpandLocation> ExpandLocations { get; }

    public bool IsNotBlockingExpand(Vector2 position);
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
