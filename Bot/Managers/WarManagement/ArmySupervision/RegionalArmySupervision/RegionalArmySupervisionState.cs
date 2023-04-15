﻿using System.Collections.Generic;
using System.Linq;
using Bot.Algorithms;
using Bot.GameSense.RegionTracking;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl;
using Bot.MapKnowledge;
using Bot.StateManagement;
using SC2APIProtocol;

namespace Bot.Managers.WarManagement.ArmySupervision.RegionalArmySupervision;

public abstract class RegionalArmySupervisionState : State<RegionalArmySupervisor> {
    public IReadOnlyCollection<Unit> EnemyArmy { protected get; set; } = new List<Unit>();
    public IReadOnlyCollection<Unit> SupervisedUnits { protected get; set; } = new List<Unit>();
    public IRegion TargetRegion { protected get; set; }
    public IUnitsControl UnitsController { protected get; set; }

    /// <summary>
    /// Gets the units that can be released.
    /// </summary>
    /// <returns>The units that can be released.</returns>
    public abstract IEnumerable<Unit> GetReleasableUnits();

    /// <summary>
    /// For each region, compute the regions that should be avoided.
    /// A region should be avoided if there is danger in it.
    /// </summary>
    /// <param name="originRegions">The regions to compute the blocked regions map for.</param>
    /// <returns>A map of blocked regions</returns>
    protected static IDictionary<IRegion, HashSet<IRegion>> ComputeBlockedRegionsMap(IReadOnlySet<IRegion> originRegions) {
        var reachableRegions = ComputeRegionsReach(originRegions);

        return originRegions.ToDictionary(
            region => region,
            region => RegionAnalyzer.Regions.Except(reachableRegions[region]).ToHashSet()
        );
    }

    /// <summary>
    /// Computes the reach of each of the provided region.
    /// A region is reachable if there's a path to it that doesn't go through a dangerous region.
    /// </summary>
    /// <param name="regions">The regions to compute the reach of.</param>
    /// <returns>A map of reachable regions</returns>
    // TODO GD Share this instead of recomputing
    protected static Dictionary<IRegion, List<IRegion>> ComputeRegionsReach(IEnumerable<IRegion> regions) {
        var reach = new Dictionary<IRegion, List<IRegion>>();
        foreach (var startingRegion in regions) {
            // TODO GD We can greatly optimize this by using dynamic programming
            reach[startingRegion] = TreeSearch.BreadthFirstSearch(
                startingRegion,
                region => region.GetReachableNeighbors().Where(neighbor => RegionTracker.GetForce(neighbor, Alliance.Enemy) <= 0),
                _ => false // TODO GD We might not need this predicate after all?
            ).ToList();
        }

        return reach;
    }
}