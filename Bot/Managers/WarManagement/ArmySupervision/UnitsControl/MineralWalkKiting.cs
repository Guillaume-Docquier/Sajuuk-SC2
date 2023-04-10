using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Algorithms;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapKnowledge;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl;

public class MineralWalkKiting : IUnitsControl {
    public bool IsExecuting() {
        // No state, we can abort anytime
        return false;
    }

    public IReadOnlySet<Unit> Execute(IReadOnlySet<Unit> army) {
        var uncontrolledUnits = new HashSet<Unit>(army);

        var droneMaximumWeaponCooldown = KnowledgeBase.GetUnitTypeData(Units.Drone).Weapons[0].Speed * TimeUtils.FramesPerSecond; // Speed is in seconds between attacks
        var dronesThatNeedToKite = army
            .Where(unit => unit.UnitType == Units.Drone)
            // We mineral walk for a duration of 75% of the weapon cooldown.
            // This can be tweaked. The rationale is that if we walk for 50% of the cooldown, the back and forth will be the same distance.
            // However, if we're gradually moving back, then the way back will be shorter and we might be in a fighting situation before our weapons are ready.
            // On the other hand, if we walk for 100% of the cooldown, our weapons will be ready but we'll be too far to attack.
            // The ideal value is most likely between 50% and 100%, so I went for 75%
            .Where(drone => drone.RawUnitData.WeaponCooldown > (1 - 0.75) * droneMaximumWeaponCooldown)
            .ToHashSet();

        if (dronesThatNeedToKite.Count == 0) {
            return uncontrolledUnits;
        }

        var regimentRegion = dronesThatNeedToKite.GetRegion();
        if (regimentRegion == null) {
            return uncontrolledUnits;
        }

        var candidateMineralFields = GetMineralFieldsToWalkTo(regimentRegion);
        if (candidateMineralFields.Count <= 0) {
            return uncontrolledUnits;
        }

        // Filter out some enemies for performance
        var regimentCenter = Clustering.GetCenter(dronesThatNeedToKite);
        var potentialEnemiesToAvoid = UnitsTracker.EnemyUnits.Where(enemy => enemy.DistanceTo(regimentCenter) <= 13).ToList();
        if (potentialEnemiesToAvoid.Count <= 0) {
            return uncontrolledUnits;
        }

        foreach (var drone in dronesThatNeedToKite) {
            if (drone.IsMineralWalking()) {
                uncontrolledUnits.Remove(drone);
                continue;
            }

            var enemiesToAvoid = potentialEnemiesToAvoid
                .Where(enemy => enemy.DistanceTo(drone.Position) <= 5)
                .OrderBy(enemy => enemy.DistanceTo(drone.Position))
                .Take(5)
                .ToList();

            if (!enemiesToAvoid.Any(enemy => enemy.CanHitGround)) {
                // This tries to handle cases where kiting would decrease the dps because we're hitting things that don't fight back (i.e buildings in construction)
                continue;
            }

            var mineralToWalkTo = GetMineralFieldToWalkTo(drone, enemiesToAvoid, candidateMineralFields);
            if (mineralToWalkTo == null) {
                continue;
            }

            drone.Gather(mineralToWalkTo);
            uncontrolledUnits.Remove(drone);
        }

        return uncontrolledUnits;
    }

    public void Reset(IReadOnlyCollection<Unit> army) {
        // No state, aborting is instantaneous
    }

    /// <summary>
    /// Gets an first non depleted expand that will route through the exist from the origin.
    /// Returns null if none were found.
    /// </summary>
    /// <param name="origin">The origin of the exit</param>
    /// <param name="exit">The exit to go through</param>
    /// <returns>An expand whose path from the origin goes through the exit, or null if none.</returns>
    private static ExpandLocation GetExpandLocationGoingThroughExit(IRegion origin, IRegion exit) {
        var exploredRegions = new HashSet<IRegion> { origin };
        var regionsToExplore = new Queue<IRegion>();
        regionsToExplore.Enqueue(exit);

        while (regionsToExplore.Count > 0) {
            var regionToExplore = regionsToExplore.Dequeue();
            exploredRegions.Add(regionToExplore);

            if (regionToExplore.Type == RegionType.Expand && !regionToExplore.ExpandLocation.IsDepleted) {
                return regionToExplore.ExpandLocation;
            }

            var unexploredNeighbors = regionToExplore
                .GetReachableNeighbors()
                .Where(neighbor => !exploredRegions.Contains(neighbor));

            foreach (var neighbor in unexploredNeighbors) {
                regionsToExplore.Enqueue(neighbor);
            }
        }

        return null;
    }

    /// <summary>
    /// Find all mineral fields that can be used to mineral walk.
    /// - Those in the current region
    /// - One patch per exit from the current region
    ///
    /// We will return a list of tuples composed of the mineral field and a position representing the exit that the unit would go through when going to that mineral field.
    /// In the case of mineral fields in the same region as the regimentPosition, their exit will be themselves because the unit won't be funneled through an exit.
    /// </summary>
    /// <param name="regimentRegion">The region the regiment currently is in</param>
    /// <returns>A list of tuples composed of the mineral field and a position representing the exit that the unit would go through when going to that mineral field.</returns>
    private static List<(Unit unit, Vector2 exit)> GetMineralFieldsToWalkTo(IRegion regimentRegion) {
        var mineralFieldsToWalkTo = new List<(Unit unit, Vector2 exit)>();

        if (regimentRegion.Type == RegionType.Expand && !regimentRegion.ExpandLocation.IsDepleted) {
            // When the region is the regimentRegion, we return all minerals with their own position as exit.
            // We keep all minerals because each of them will likely be in a different direction from the unit.
            var minerals = regimentRegion.ExpandLocation.ResourceCluster
                .Where(resource => Units.MineralFields.Contains(resource.UnitType))
                .Select(resource => (resource, resource.Position.ToVector2()));

            mineralFieldsToWalkTo.AddRange(minerals);
        }

        var regimentRegionExits = regimentRegion.GetReachableNeighbors();
        foreach (var regimentRegionExit in regimentRegionExits) {
            var expandLocation = GetExpandLocationGoingThroughExit(regimentRegion, regimentRegionExit);
            if (expandLocation == null) {
                continue;
            }

            // When the region is not the regimentRegion, it means we'll have to go through an exit to get to the mineral field.
            // Because of this, all minerals will more or less create the same path to that exit, so we'll just pick one.
            // The direction of the drone moving towards that mineral is hard to accurately determine, so we'll use the center of the exit as an estimation.
            var mineral = expandLocation.ResourceCluster.FirstOrDefault(resource => Units.MineralFields.Contains(resource.UnitType));
            if (mineral == null) {
                continue;
            }

            mineralFieldsToWalkTo.Add((mineral, regimentRegionExit.Center));
        }

        return mineralFieldsToWalkTo;
    }

    /// <summary>
    /// Gets the mineral from mineralFields that the given drone should to walk to in order to avoid enemiesToAvoid.
    /// </summary>
    /// <param name="drone">The drone that needs to run</param>
    /// <param name="enemiesToAvoid">The enemies that need to be avoided</param>
    /// <param name="mineralFields">The mineral fields we can walk to</param>
    /// <returns>The mineral field to walk to, or null if there are no good choices</returns>
    private static Unit GetMineralFieldToWalkTo(Unit drone, IReadOnlyCollection<Unit> enemiesToAvoid, IEnumerable<(Unit unit, Vector2 exit)> mineralFields) {
        if (enemiesToAvoid.Count <= 0) {
            return null;
        }

        // Get the enemy-drone vector
        var enemiesCenter = Clustering.GetCenter(enemiesToAvoid);
        var enemyVector = enemiesCenter.DirectionTo(drone.Position);

        // Find the mineral field with the minimum angle to the enemy-drone vector (angle 0 = directly fleeing the enemy
        var mineralToWalkTo = mineralFields.MinBy(mineralField => {
            var mineralVector = drone.Position.ToVector2().DirectionTo(mineralField.exit);

            return Math.Abs(enemyVector.GetRadAngleTo(mineralVector));
        });

        var mineralVector = drone.Position.ToVector2().DirectionTo(mineralToWalkTo.exit);
        var mineralAngle = Math.Abs(enemyVector.GetRadAngleTo(mineralVector));
        if (mineralAngle > MathUtils.DegToRad(90)) {
            DebugMineralWalkAngle(drone, enemiesCenter, mineralToWalkTo.exit, mineralAngle, Colors.Red);

            // If the best we got sends us towards the enemy, we don't do it
            return null;
        }

        DebugMineralWalkAngle(drone, enemiesCenter, mineralToWalkTo.exit, mineralAngle, Colors.BrightGreen);

        return mineralToWalkTo.unit;
    }

    private static void DebugMineralWalkAngle(Unit drone, Vector2 enemyCenter, Vector2 mineralExit, double mineralAngle, Color color) {
        Program.GraphicalDebugger.AddText($"{MathUtils.RadToDeg(mineralAngle):F0} deg", worldPos: drone.Position.ToPoint(zOffset: 2), color: color);

        Program.GraphicalDebugger.AddSphere(mineralExit.ToVector3(zOffset: 2), 0.5f, Colors.Cyan);
        Program.GraphicalDebugger.AddLine(drone.Position.Translate(zTranslation: 2), mineralExit.ToVector3(zOffset: 2), Colors.Cyan);
        Program.GraphicalDebugger.AddSphere(drone.Position.Translate(zTranslation: 2), drone.Radius, color);
        Program.GraphicalDebugger.AddLine(drone.Position.Translate(zTranslation: 2), enemyCenter.ToVector3(zOffset: 2), Colors.Magenta);
        Program.GraphicalDebugger.AddSphere(enemyCenter.ToVector3(zOffset: 2), 0.5f, Colors.Magenta);
    }
}
