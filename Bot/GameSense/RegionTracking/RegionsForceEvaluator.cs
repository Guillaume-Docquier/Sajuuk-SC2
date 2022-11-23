using System;
using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.MapKnowledge;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.GameSense.RegionTracking;

public class RegionsForceEvaluator : IRegionsEvaluator {
    private readonly Alliance _alliance;
    private readonly bool _hasAbsoluteKnowledge;
    private Dictionary<Region, float> _regionForces;
    private Dictionary<Region, float> _normalizedRegionForces;

    // It would be cool to use the exponential decay everywhere
    // But that would require tracking the last force update of each region
    // And we would be doing some fancy (expensive?) exponent operations
    // But in the end, a good ol' factor works perfectly well, so... maybe some other day
    private static readonly float RegionDecayRate = 1f - 1f / TimeUtils.SecsToFrames(60);

    private static readonly ulong HalfLife = TimeUtils.SecsToFrames(60);
    private static readonly double ExponentialDecayConstant = Math.Log(2) / HalfLife;

    public RegionsForceEvaluator(Alliance alliance) {
        _alliance = alliance;
        _hasAbsoluteKnowledge = alliance == Alliance.Self;
    }

    /// <summary>
    /// Gets the evaluated force of the provided region
    /// </summary>
    /// <param name="region">The region to get the evaluated force of</param>
    /// <param name="normalized">Whether or not to get the normalized value between 0 and 1.</param>
    /// <returns>The evaluated force of the region</returns>
    public float GetEvaluation(Region region, bool normalized = false) {
        if (region == null || !_regionForces.ContainsKey(region)) {
            Logger.Error("Trying to get the force of an unknown region: {0}. {1} regions are known.", region, _regionForces.Count);
            return UnitEvaluator.Force.Unknown;
        }

        if (normalized) {
            return _normalizedRegionForces[region];
        }

        return _regionForces[region];
    }

    /// <summary>
    /// Initializes the region forces of the provided regions
    /// </summary>
    public void Init(List<Region> regions) {
        var initialForce = _hasAbsoluteKnowledge
            ? UnitEvaluator.Force.None
            : UnitEvaluator.Force.Unknown;

        _regionForces = new Dictionary<Region, float>();
        foreach (var region in regions) {
            _regionForces[region] = initialForce;
        }
    }

    /// <summary>
    /// Evaluates the force of each region based on the units and fog of war
    /// The force decays as the information grows older
    /// </summary>
    public void Evaluate() {
        // Update based on units
        var newRegionForces = ComputeUnitsForce();

        // Update based on visibility
        foreach (var (region, fogOfWarDanger) in ComputeFogOfWarDanger()) {
            if (!newRegionForces.ContainsKey(region)) {
                newRegionForces[region] = fogOfWarDanger;
            }
            else {
                newRegionForces[region] += fogOfWarDanger;
            }
        }

        if (!_hasAbsoluteKnowledge) {
            var enemySpawnForceCue = GetSpawnForceCue();
            if (!newRegionForces.ContainsKey(enemySpawnForceCue.Region)) {
                newRegionForces[enemySpawnForceCue.Region] = enemySpawnForceCue.Force;
            }
            else {
                newRegionForces[enemySpawnForceCue.Region] = Math.Max(enemySpawnForceCue.Force, newRegionForces[enemySpawnForceCue.Region]);
            }
        }

        // Update the forces
        foreach (var (region, newRegionForce) in newRegionForces) {
            // Let dangerous force decay over time
            if (newRegionForce > UnitEvaluator.Force.Neutral) {
                _regionForces[region] = Math.Max(_regionForces[region], newRegionForce);
            }
            // Let safe force decay over time
            else {
                _regionForces[region] = Math.Min(_regionForces[region], newRegionForce);
            }
        }

        if (!_hasAbsoluteKnowledge) {
            DecayForces();
        }

        _normalizedRegionForces = ComputeNormalizedForces(_regionForces);
    }

    /// <summary>
    /// For each region, compute the force of the units within
    /// </summary>
    /// <returns>A force score for each region</returns>
    private Dictionary<Region, float> ComputeUnitsForce() {
        var regionForces = new Dictionary<Region, float>();
        var unitsToConsider = UnitsTracker.GetUnits(_alliance).Concat(UnitsTracker.GetGhostUnits(_alliance));
        foreach (var unit in unitsToConsider) {
            var region = unit.Position.GetRegion();
            if (region == null) {
                continue;
            }

            var force = UnitEvaluator.EvaluateForce(unit) * GetUnitUncertaintyPenalty(unit);
            if (!regionForces.ContainsKey(region)) {
                regionForces[region] = force;
            }
            else {
                regionForces[region] += force;
            }
        }

        return regionForces;
    }

    /// <summary>
    /// Penalize the force of units (like ghost units) that have not been seen in a while.
    /// </summary>
    /// <param name="enemy"></param>
    /// <returns>The force penalty, within ]0, 1]</returns>
    private static float GetUnitUncertaintyPenalty(Unit enemy) {
        // TODO GD Maybe make terran building that can fly uncertain, but not so much
        // Buildings can be considered static
        if (Units.Buildings.Contains(enemy.UnitType)) {
            return 1;
        }

        // Avoid computing decay for nothing
        if (Controller.Frame == enemy.LastSeen) {
            return 1;
        }

        return ExponentialDecayFactor(Controller.Frame - enemy.LastSeen);
    }

    /// <summary>
    /// For each region, return some danger based on the non-visible portion of the region.
    /// </summary>
    /// <returns>The danger level of each region based on the visible percentage of the region</returns>
    private Dictionary<Region, float> ComputeFogOfWarDanger() {
        if (_hasAbsoluteKnowledge) {
            return new Dictionary<Region, float>();
        }

        var regionVisibility = new Dictionary<Region, int>();
        foreach (var visibleCell in VisibilityTracker.VisibleCells) {
            var region = visibleCell.GetRegion();
            if (region == null) {
                continue;
            }

            if (!regionVisibility.ContainsKey(region)) {
                regionVisibility[region] = 1;
            }
            else {
                regionVisibility[region] += 1;
            }
        }

        var fogOfWarDanger = new Dictionary<Region, float>();
        foreach (var (region, visibleCellCount) in regionVisibility) {
            var percentNotVisible = 1 - (float)visibleCellCount / region.Cells.Count;
            fogOfWarDanger[region] = percentNotVisible * UnitEvaluator.Force.Unknown;
        }

        return fogOfWarDanger;
    }

    /// <summary>
    /// Returns the exponential decay factor of a value after some time
    /// </summary>
    /// <param name="time">The time since decay started</param>
    /// <returns>The decay factor</returns>
    private static float ExponentialDecayFactor(ulong time) {
        return (float)Math.Exp(-ExponentialDecayConstant * time);
    }

    /// <summary>
    /// Gets a force cue for the spawn.
    /// This is because we know where the enemy is, but the bot doesn't see it.
    /// </summary>
    /// <returns></returns>
    private (Region Region, float Force) GetSpawnForceCue() {
        var spawnRegion = ExpandAnalyzer.GetExpand(_alliance, ExpandType.Main).Position.GetRegion();
        var spawnRegionExplorationPercentage = (float)spawnRegion.Cells.Count(VisibilityTracker.IsExplored) / spawnRegion.Cells.Count;

        // No cue if we've explored it
        if (spawnRegionExplorationPercentage > 0.6) {
            return (spawnRegion, 0);
        }

        var force = UnitEvaluator.Force.Lethal * (1 - spawnRegionExplorationPercentage);
        return (spawnRegion, force);
    }

    /// <summary>
    /// Decays the forces to represent uncertainty over time
    /// </summary>
    private void DecayForces() {
        // Decay towards Neutral over time
        foreach (var region in _regionForces.Keys) {
            var normalizedTowardsNeutralForce = _regionForces[region] - UnitEvaluator.Force.Neutral;
            var decayedForce = normalizedTowardsNeutralForce * RegionDecayRate + UnitEvaluator.Force.Neutral;
            if (Math.Abs(UnitEvaluator.Force.Neutral - decayedForce) < 0.05) {
                decayedForce = UnitEvaluator.Force.Neutral;
            }

            _regionForces[region] = decayedForce;
        }
    }

    /// <summary>
    /// Normalizes the given forces to put them between 0 and 1.
    /// </summary>
    /// <param name="forces">The forces to normalize</param>
    /// <returns>A new dictionary with the forces normalized</returns>
    private static Dictionary<Region, float> ComputeNormalizedForces(Dictionary<Region, float> forces) {
        var minForce = forces.Values.Min();
        var maxForce = forces.Values.Max();

        var normalizedForces = new Dictionary<Region, float>();
        foreach (var region in forces.Keys) {
            normalizedForces[region] = MathUtils.Normalize(forces[region], minForce, maxForce);
        }

        return normalizedForces;
    }
}
