using System;
using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapKnowledge;
using Bot.Utils;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl;

public class DroneKitingUnitsControl : IUnitsControl {
    public bool IsExecuting() {
        // No state, we can abort anytime
        return false;
    }

    public IReadOnlySet<Unit> Execute(IReadOnlySet<Unit> army) {
        var uncontrolledUnits = new HashSet<Unit>(army);

        var dronesThatNeedToKite = army
            .Where(unit => unit.UnitType == Units.Drone)
            .Where(drone => drone.RawUnitData.WeaponCooldown > 0)
            .ToHashSet();

        if (dronesThatNeedToKite.Count == 0) {
            return uncontrolledUnits;
        }

        var regimentCenter = Clustering.GetCenter(dronesThatNeedToKite);
        var regimentRegion = regimentCenter.GetRegion();

        var regimentRegionExits = regimentRegion.GetReachableNeighbors();
        var mineralFields = regimentRegionExits.SelectMany(regimentRegionExit => {
            // TODO GD We can reduce the amount of mineral fields by returning only 1 if we need to go through an exit
            // TODO GD We should also consider the location of such minerals fields to be the exit region, since we'll route through there
            var expandLocation = GetExpandLocationGoingThroughExit(regimentRegion, regimentRegionExit);
            if (expandLocation != null) {
                return expandLocation.ResourceCluster.Where(resource => Units.MineralFields.Contains(resource.UnitType));
            }

            return Enumerable.Empty<Unit>();
        })
            .ToList();

        // No mineral fields to walk to, we can't act.
        if (mineralFields.Count <= 0) {
            return uncontrolledUnits;
        }

        // Filter out some enemies for performance
        var potentialEnemiesToAvoid = UnitsTracker.EnemyUnits.Where(enemy => enemy.DistanceTo(regimentCenter) <= 20).ToList();
        if (potentialEnemiesToAvoid.Count <= 0) {
            return uncontrolledUnits;
        }

        foreach (var drone in dronesThatNeedToKite) {
            if (drone.IsMineralWalking()) {
                uncontrolledUnits.Remove(drone);
                continue;
            }

            var enemiesToAvoid = potentialEnemiesToAvoid.Where(enemy => enemy.DistanceTo(regimentCenter) <= 5).ToList();
            var mineralToWalkTo = GetMineralToWalkTo(drone, enemiesToAvoid, mineralFields);

            if (mineralToWalkTo != null) {
                drone.Gather(mineralToWalkTo);
                uncontrolledUnits.Remove(drone);
            }
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
    private static ExpandLocation GetExpandLocationGoingThroughExit(Region origin, Region exit) {
        var exploredRegions = new HashSet<Region> { origin };
        var regionsToExplore = new Queue<Region>();
        regionsToExplore.Enqueue(exit);

        while (regionsToExplore.Count > 0) {
            var regionToExplore = regionsToExplore.Dequeue();
            exploredRegions.Add(regionToExplore);

            if (regionToExplore.Type == RegionType.Expand && !regionToExplore.ExpandLocation.IsDepleted) {
                return regionToExplore.ExpandLocation;
            }

            foreach (var neighbor in regionToExplore.GetReachableNeighbors().Where(neighbor => !exploredRegions.Contains(neighbor))) {
                regionsToExplore.Enqueue(neighbor);
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the mineral from mineralFields that the given drone should to walk to in order to avoid enemiesToAvoid.
    /// </summary>
    /// <param name="drone">The drone that needs to run</param>
    /// <param name="enemiesToAvoid">The enemies that need to be avoided</param>
    /// <param name="mineralFields">The mineral fields we can walk to</param>
    /// <returns>The mineral field to walk to, or null if there are no good choices</returns>
    private static Unit GetMineralToWalkTo(Unit drone, IReadOnlyCollection<Unit> enemiesToAvoid, IEnumerable<Unit> mineralFields) {
        if (enemiesToAvoid.Count <= 0) {
            return null;
        }

        // Get the enemy-drone angle
        var enemiesCenter = Clustering.GetCenter(enemiesToAvoid);
        var enemyVector = enemiesCenter.DirectionTo(drone.Position);

        // Find the mineral field with the angle closest to enemy-drone (angle 0 = directly fleeing the enemy
        var mineralToWalkTo = mineralFields.MinBy(mineralField => {
            var mineralVector = drone.Position.DirectionTo(mineralField.Position);

            return Math.Abs(enemyVector.GetRadAngleTo(mineralVector));
        });

        var mineralVector = drone.Position.DirectionTo(mineralToWalkTo.Position);
        var mineralAngle = Math.Abs(enemyVector.GetRadAngleTo(mineralVector));
        if (mineralAngle > MathUtils.DegToRad(90)) {
            // If the best we got sends us towards the enemy, we don't do it
            return null;
        }

        return mineralToWalkTo;
    }
}
