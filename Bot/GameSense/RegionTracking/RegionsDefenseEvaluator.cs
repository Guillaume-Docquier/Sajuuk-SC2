using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.MapKnowledge;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.GameSense.RegionTracking;

using ReachMap = Dictionary<Region, float>;

// TODO GD Highlight the dependency to Force and Value evaluators
public class RegionDefenseEvaluator : IRegionsEvaluator {
    private Dictionary<Region, float> _regionDefenseScores;

    /// <summary>
    /// Gets the defense score of the provided region
    /// </summary>
    /// <param name="region">The region to get the evaluated value of</param>
    /// <param name="normalized">Whether or not to get the normalized value between 0 and 1. NOT IMPLEMENTED</param>
    /// <returns>The evaluated value of the region</returns>
    public float GetEvaluation(Region region, bool normalized = false) {
        if (!_regionDefenseScores.ContainsKey(region)) {
            Logger.Error("Trying to get the value of an unknown region. {0} regions are known.", _regionDefenseScores.Count);
            return 0;
        }

        return _regionDefenseScores[region];
    }

    /// <summary>
    /// Initializes the evaluator to track the provided regions.
    /// </summary>
    /// <param name="regions">The regions to evaluate in the future</param>
    public void Init(List<Region> regions) {
        _regionDefenseScores = new Dictionary<Region, float>();
        foreach (var region in regions) {
            _regionDefenseScores[region] = 0f;
        }
    }

    /// <summary>
    /// Evaluates the defense score of each region.
    /// A defense score indicates how important defending a region is. The higher, the better.
    /// </summary>
    public void Evaluate() {
        // TODO GD Consider the dangerous regions instead of the static spawn
        var enemySpawnRegion = ExpandAnalyzer.GetExpand(Alliance.Enemy, ExpandType.Main).Position.GetRegion();
        var allRegions = _regionDefenseScores.Keys.ToList();

        foreach (var defendedRegion in allRegions) {
            _regionDefenseScores[defendedRegion] = ComputeDefenseScore(enemySpawnRegion, defendedRegion, allRegions);
        }
    }

    /// <summary>
    /// Computes the
    /// </summary>
    /// <param name="regionToDefendAgainst"></param>
    /// <param name="regionToDefendFrom"></param>
    /// <param name="allRegions"></param>
    /// <returns></returns>
    private static float ComputeDefenseScore(Region regionToDefendAgainst, Region regionToDefendFrom, List<Region> allRegions) {
        // compute the enemy reach as if the defendedRegion was blocked
        var enemyReach = ComputeReach(regionToDefendAgainst, allRegions, regionToDefendFrom);
        var ourReach = ComputeReach(regionToDefendFrom, allRegions);

        // Give a defense score based on the other regions values, our distance to them and the enemy distance to them
        // If the enemy cannot reach but we can, it's very good
        // If we're closer than the enemy, that's also good
        var regionDefenseScore = 0f;
        foreach (var region in allRegions) {
            regionDefenseScore += ComputeDefenseImpactScore(region, ourReach, enemyReach);
        }

        return regionDefenseScore;
    }

    /// <summary>
    /// Compute the reach from a region to all others.
    /// The reach is the shortest distance from a region to another.
    /// If a region is not present in the resulting ReachMap, it is unreachable from the starting position.
    /// </summary>
    /// <param name="startingRegion"></param>
    /// <param name="regions"></param>
    /// <param name="blockedRegion"></param>
    /// <returns></returns>
    private static ReachMap ComputeReach(Region startingRegion, IEnumerable<Region> regions, Region blockedRegion = null) {
        var reach = new Dictionary<Region, float>
        {
            [startingRegion] = 0
        };

        var blockedRegionsHashSet = new HashSet<Region>();
        if (blockedRegion != null) {
            blockedRegionsHashSet.Add(blockedRegion);
        }

        // We pathfind the farthest regions first to leverage the pathfinding cache
        var regionsToFindPath = regions
            .Where(region => region != startingRegion)
            .Except(blockedRegionsHashSet)
            .OrderByDescending(region => region.Center.DistanceTo(startingRegion.Center));

        foreach (var region in regionsToFindPath) {
            var path = Pathfinder.FindPath(startingRegion, region, blockedRegionsHashSet);
            if (path != null) {
                reach[region] = path.GetPathDistance();
            }
        }

        return reach;
    }

    /// <summary>
    /// Computes the defense impact on a given region by comparing the defense reach and the enemy (attack) reach.
    /// The defended region(s) are encoded in the defenseReach.
    /// A defense has a great impact if it is closer than the enemy and especially if it completely blocks the enemy out.
    /// </summary>
    /// <param name="impactedRegion">The region impacted by the defense</param>
    /// <param name="defenseReach">The reach of the defensive positions</param>
    /// <param name="enemyReach">The reach of the enemy</param>
    /// <returns>A score within [0, 3] representing the impact of the defensive positions on this region</returns>
    private static float ComputeDefenseImpactScore(Region impactedRegion, ReachMap defenseReach, ReachMap enemyReach) {
        // We cannot reach this region, therefore it isn't factored in our defense score
        // TODO GD We could still factor it in to plan in the future, but with some penalty for not being reachable
        if (!defenseReach.ContainsKey(impactedRegion)) {
            return 0;
        }

        var regionValue = RegionTracker.GetValue(impactedRegion, Alliance.Self);
        // TODO GD Maybe include intriguing?
        if (regionValue <= RegionTracker.Value.Intriguing) {
            // Regions with low value are not considered
            return 0;
        }

        // Value score from 0 to 1
        var valueScore = RegionTracker.GetValue(impactedRegion, Alliance.Self, normalized: true);

        // All distances will be skewed by 1 to avoid division by 0
        var enemyMaxReach = enemyReach.Values.Max() + 1;
        var ourMaxReach = defenseReach.Values.Max() + 1;

        // Will be used to normalize the distance score
        var minDistanceRatio = 1 / ourMaxReach;
        var maxDistanceRatio = enemyMaxReach / 1;

        // If the enemy cannot reach but we can, give the max distance score plus a bonus
        // (The enemy most likely cannot reach because we are in the way, this is great positioning)
        var enemyDistance = enemyMaxReach;
        var obstructionBonus = 1f;
        if (enemyReach.ContainsKey(impactedRegion)) {
            // All distances will be skewed by 1 to avoid division by 0
            enemyDistance = enemyReach[impactedRegion] + 1;
            obstructionBonus = 0;
        }

        // All distances will be skewed by 1 to avoid division by 0
        var ourDistance = defenseReach[impactedRegion] + 1;

        // Distance score from 0 to 1. 1 being when we're close and the enemy is far
        var distanceScore = MathUtils.Normalize(enemyDistance / ourDistance, minDistanceRatio, maxDistanceRatio);

        // Give a defense impact score based on the other region value, our distance to them and the enemy distance to them
        // Each value is normalized, giving a score in [0, 3] for each region
        return valueScore + distanceScore + obstructionBonus;
    }
}
