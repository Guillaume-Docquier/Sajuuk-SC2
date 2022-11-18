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

    // It would be cool to use the exponential decay everywhere
    // But that would require tracking the last force update of each region
    // And we would be doing some fancy (expensive?) exponent operations
    // But in the end, a good ol' factor works perfectly well, so... maybe some other day
    private static readonly float RegionDecayRate = 1f - 1f / TimeUtils.SecsToFrames(15);

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
    /// <param name="normalized">Whether or not to get the normalized value between 0 and 1. NOT IMPLEMENTED</param>
    /// <returns>The evaluated force of the region</returns>
    public float GetEvaluation(Region region, bool normalized = false) {
        if (!_regionForces.ContainsKey(region)) {
            Logger.Error("Trying to get the force of an unknown region. {0} regions are known.", _regionForces.Count);
            return RegionTracker.Force.Unknown;
        }

        return _regionForces[region];
    }

    /// <summary>
    /// Initializes the region forces of the provided regions
    /// </summary>
    public void Init(List<Region> regions) {
        var initialForce = _hasAbsoluteKnowledge
            ? RegionTracker.Force.None
            : RegionTracker.Force.Unknown;

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
            // Update the enemy spawn
            var spawnForce = GetEnemySpawnForceCue();
            if (!newRegionForces.ContainsKey(spawnForce.Region)) {
                newRegionForces[spawnForce.Region] = spawnForce.Force;
            }
            else {
                newRegionForces[spawnForce.Region] = Math.Max(spawnForce.Force, newRegionForces[spawnForce.Region]);
            }
        }

        // Update the forces
        foreach (var (region, newRegionForce) in newRegionForces) {
            // Let dangerous force decay over time
            if (newRegionForce > RegionTracker.Force.Neutral) {
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

            var force = GetUnitForce(unit) * GetUnitUncertaintyPenalty(unit);
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
    /// Get the force of a unit
    /// TODO GD Make this more sophisticated (based on unit cost, probably)
    /// </summary>
    /// <param name="unit">The dangerous unit</param>
    /// <returns>The force of the unit</returns>
    private float GetUnitForce(Unit unit) {
        // TODO GD This should be more nuanced, a lone dropship is more dangerous than a dropship with a visible army
        if (Units.DropShips.Contains(unit.UnitType)) {
            return RegionTracker.Force.Strong;
        }

        if (Units.CreepTumors.Contains(unit.UnitType)) {
            return RegionTracker.Force.Medium / 16;
        }

        if (Units.Military.Contains(unit.UnitType)) {
            return RegionTracker.Force.Medium / 2;
        }

        if (Units.Workers.Contains(unit.UnitType)) {
            return RegionTracker.Force.Medium / 8;
        }

        if (Units.StaticDefenses.Contains(unit.UnitType)) {
            return RegionTracker.Force.Medium;
        }

        if (unit.UnitType == Units.Pylon) {
            if (IsProxyPylon(unit, _alliance)) {
                return RegionTracker.Force.Medium;
            }

            return RegionTracker.Force.Medium / 3;
        }

        // The rest are buildings
        // TODO GD Consider production buildings as somewhat dangerous (they produce units)
        return RegionTracker.Force.None;
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
            fogOfWarDanger[region] = percentNotVisible * RegionTracker.Force.Unknown;
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
    /// Determines if this pylon of ours is a proxy pylon
    /// My proxy pylon is closer to them than it is to me
    /// </summary>
    /// <param name="pylon"></param>
    /// <param name="myAlliance"></param>
    /// <returns></returns>
    private static bool IsProxyPylon(Unit pylon, Alliance myAlliance) {
        var mains = ExpandAnalyzer.ExpandLocations.Where(expand => expand.ExpandType == ExpandType.Main);
        var myMain = ExpandAnalyzer.GetExpand(myAlliance, ExpandType.Main);
        var theirMain = mains.First(main => main != myMain);

        return pylon.DistanceTo(theirMain.Position) < pylon.DistanceTo(myMain.Position);
    }

    /// <summary>
    /// Gets a force cue for the enemy spawn.
    /// This is because we know where the enemy is, but the bot doesn't see it.
    /// </summary>
    /// <returns></returns>
    private static (Region Region, float Force) GetEnemySpawnForceCue() {
        var enemySpawnRegion = MapAnalyzer.EnemyStartingLocation.GetRegion();
        var enemySpawnRegionExplorationPercentage = (float)VisibilityTracker.ExploredCells.Count(exploredCell => enemySpawnRegion.Cells.Contains(exploredCell)) / enemySpawnRegion.Cells.Count;
        if (enemySpawnRegionExplorationPercentage < 0.6) {
            var force = RegionTracker.Force.Lethal * (1 - enemySpawnRegionExplorationPercentage);
            return (enemySpawnRegion, force);
        }

        return (enemySpawnRegion, 0);
    }

    /// <summary>
    /// Decays the forces to represent uncertainty over time
    /// </summary>
    private void DecayForces() {
        // Decay towards Neutral over time
        foreach (var region in _regionForces.Keys) {
            var normalizedTowardsNeutralForce = _regionForces[region] - RegionTracker.Force.Neutral;
            var decayedForce = normalizedTowardsNeutralForce * RegionDecayRate + RegionTracker.Force.Neutral;
            if (Math.Abs(RegionTracker.Force.Neutral - decayedForce) < 0.05) {
                decayedForce = RegionTracker.Force.Neutral;
            }

            _regionForces[region] = decayedForce;
        }
    }
}
