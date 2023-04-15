using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.MapKnowledge;

namespace Bot.Managers.WarManagement.ArmySupervision.RegionalArmySupervision;

public class ApproachState : RegionalArmySupervisionState {
    /// <summary>
    /// Move all units into striking position to prepare for an assault.
    /// Units will only route through safe regions and stay at a safe distance of enemies in the target region.
    /// </summary>
    protected override void Execute() {
        MoveIntoStrikingPosition(SupervisedUnits, TargetRegion, EnemyArmy);
    }

    /// <summary>
    /// Transition to EngageState once enough units are in striking position.
    /// </summary>
    /// <returns>True if the transition happened, false otherwise</returns>
    protected override bool TryTransitioning() {
        var unitsInStrikingPosition = GetUnitsInStrikingPosition(SupervisedUnits, TargetRegion, EnemyArmy);
        if (unitsInStrikingPosition.GetForce() < EnemyArmy.GetForce()) {
            return false;
        }

        StateMachine.TransitionTo(new EngageState());
        return true;
    }

    /// <summary>
    /// All units are free to go
    /// </summary>
    /// <returns>The units that can be released</returns>
    public override IEnumerable<Unit> GetReleasableUnits() {
        return SupervisedUnits;
    }

    /// <summary>
    /// Gets all the units that are in position and ready to attack the target region.
    /// </summary>
    /// <param name="supervisedUnits">The units to consider</param>
    /// <param name="targetRegion">The region to strike</param>
    /// <param name="enemyArmy">The enemy army to strike</param>
    /// <returns>The units that are ready to fight</returns>
    private static IEnumerable<Unit> GetUnitsInStrikingPosition(IEnumerable<Unit> supervisedUnits, IRegion targetRegion, IReadOnlyCollection<Unit> enemyArmy) {
        return supervisedUnits
            .Where(unit => {
                if (unit.GetRegion() == targetRegion) {
                    return true;
                }

                // TODO GD We do this computation twice per frame, cache it!
                var closestEnemy = enemyArmy.MinBy(enemy => enemy.DistanceTo(unit) - enemy.MaxRange);

                // TODO GD We defined the striking distance as 2 * the max range of the closest enemy
                // TODO GD We add 2 as a buffer in case units clump up. We should be able to handle this better, but it should work for now
                return closestEnemy != null && closestEnemy.DistanceTo(unit) < (closestEnemy.MaxRange + 3) + 2;
            })
            .ToHashSet();
    }

    /// <summary>
    /// Moves units into strike range by moving through safe regions.
    /// Units will avoid engaging the enemy.
    /// </summary>
    /// <param name="units">The units to move</param>
    /// <param name="targetRegion">The region to go to</param>
    /// <param name="enemyArmy">The enemy units to get in range of but avoid engaging</param>
    private static void MoveIntoStrikingPosition(IReadOnlyCollection<Unit> units, IRegion targetRegion, IReadOnlyCollection<Unit> enemyArmy) {
        var approachRegions = targetRegion.GetReachableNeighbors().ToList();

        var regionsWithFriendlyUnitPresence = units
            .Select(unit => unit.GetRegion())
            .Where(region => region != null)
            .ToHashSet();

        var regionsOutOfReach = ComputeBlockedRegionsMap(regionsWithFriendlyUnitPresence);

        var unitGroups = units
            .Where(unit => unit.GetRegion() != null)
            .GroupBy(unit => approachRegions.MinBy(approachRegion => {
                var unitRegion = unit.GetRegion();
                var blockedRegions = regionsOutOfReach[unitRegion];
                if (blockedRegions.Contains(approachRegion)) {
                    return float.MaxValue;
                }

                return Pathfinder.FindPath(unitRegion, approachRegion, blockedRegions).GetPathDistance();
            }));

        foreach (var unitGroup in unitGroups) {
            MoveTowards(unitGroup, targetRegion, unitGroup.Key, regionsOutOfReach, enemyArmy);
        }
    }

    /// <summary>
    /// Moves towards the target region by going through the approach region.
    /// Units will only move through safe regions.
    /// Units will avoid engaging the enemy.
    /// </summary>
    /// <param name="units">The units to move</param>
    /// <param name="targetRegion">The region to go to</param>
    /// <param name="approachRegion">The region to go through before going towards the target region</param>
    /// <param name="blockedRegions">The regions to avoid going through</param>
    /// <param name="enemyArmy">The enemy units to get in range of but avoid engaging</param>
    private static void MoveTowards(IEnumerable<Unit> units, IRegion targetRegion, IRegion approachRegion, IDictionary<IRegion, HashSet<IRegion>> blockedRegions, IReadOnlyCollection<Unit> enemyArmy) {
        foreach (var unit in units) {
            // TODO GD We do this computation twice per frame, cache it!
            var closestEnemy = enemyArmy.MinBy(enemy => enemy.DistanceTo(unit) - enemy.MaxRange);
            // TODO GD You know what they say about magic numbers!
            if (closestEnemy != null && closestEnemy.DistanceTo(unit) < closestEnemy.MaxRange + 3) {
                // TODO GD We should avoid cornering ourselves, maybe we should go towards a region exit?
                unit.MoveAwayFrom(closestEnemy.Position.ToVector2(), closestEnemy.MaxRange + 3);
                continue;
            }

            var unitRegion = unit.GetRegion();

            if (unitRegion == approachRegion) {
                unit.Move(targetRegion.Center);
                continue;
            }

            var path = Pathfinder.FindPath(unitRegion, approachRegion, blockedRegions[unitRegion]);
            if (path == null) {
                // Trying to gracefully handle a case that I don't think should happen
                unit.Move(approachRegion.Center);
                continue;
            }

            var nextRegion = path
                .Skip(1)
                .First();

            unit.Move(nextRegion.Center);
        }
    }
}
