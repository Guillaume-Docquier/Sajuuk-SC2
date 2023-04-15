using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapKnowledge;

namespace Bot.Managers.WarManagement.ArmySupervision.RegionalArmySupervision;

public class DisengageState : RegionalArmySupervisionState {
    private IReadOnlyCollection<Unit> _unitsInSafePosition = new List<Unit>();

    /// <summary>
    /// Evacuate units from the target region and keep rallying other units to a safe position.
    /// Units will only route through safe regions and stay at a safe distance of enemies in the target region.
    /// </summary>
    protected override void Execute() {
        // TODO GD Split into two groups, units that must evacuate, and units that must rally
        MoveIntoSafePosition(SupervisedUnits, TargetRegion, EnemyArmy);
    }

    /// <summary>
    /// Transition to ApproachState if all our units are safe.
    /// </summary>
    /// <returns>True if the transition happened, false otherwise</returns>
    protected override bool TryTransitioning() {
        _unitsInSafePosition = GetUnitsInSafePosition(SupervisedUnits, EnemyArmy);
        if (_unitsInSafePosition.Count < SupervisedUnits.Count) {
            return false;
        }

        StateMachine.TransitionTo(new ApproachState());
        return true;
    }

    /// <summary>
    /// All safe units are free to go
    /// </summary>
    /// <returns>The units that can be released</returns>
    public override IEnumerable<Unit> GetReleasableUnits() {
        return _unitsInSafePosition;
    }

    /// <summary>
    /// Gets all the units that are in a safe position
    /// </summary>
    /// <param name="supervisedUnits">The units to consider</param>
    /// <param name="enemyArmy">The enemy army to strike</param>
    /// <returns>The units that are in a safe position</returns>
    private static HashSet<Unit> GetUnitsInSafePosition(IEnumerable<Unit> supervisedUnits, IReadOnlyCollection<Unit> enemyArmy) {
        return supervisedUnits
            .Where(unit => {
                // TODO GD We do this computation twice per frame, cache it!
                var closestEnemy = enemyArmy.MinBy(enemy => enemy.DistanceTo(unit) - enemy.MaxRange);

                // TODO GD We defined the striking distance as 3 + the max range of the closest enemy
                return closestEnemy == null || closestEnemy.DistanceTo(unit) >= closestEnemy.MaxRange + 3;
            })
            .ToHashSet();
    }

    /// <summary>
    /// Moves units out of the region to evacuate at a safe distance of any danger.
    /// Units will avoid engaging the enemy.
    /// </summary>
    /// <param name="units">The units to move</param>
    /// <param name="regionToEvacuate">The region to evacuate</param>
    /// <param name="enemyArmy">The enemy units to get out of range of</param>
    private static void MoveIntoSafePosition(IReadOnlyCollection<Unit> units, IRegion regionToEvacuate, IReadOnlyCollection<Unit> enemyArmy) {
        var approachRegions = regionToEvacuate.GetReachableNeighbors().ToList();

        var regionToGetTheReachOf = units
            .Select(unit => unit.GetRegion())
            .Where(region => region != null)
            .Concat(approachRegions)
            .ToHashSet();

        // TODO Merge both in one
        var regionsOutOfReach = ComputeBlockedRegionsMap(regionToGetTheReachOf);
        var regionsInReach = ComputeRegionsReach(regionToGetTheReachOf);

        var safeRegions = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.TownHalls)
            .Select(townHall => townHall.GetRegion())
            .ToHashSet();

        // TODO GD If there's no viable exit, we should fight instead of fleeing
        approachRegions = approachRegions
            .Where(approachRegion => safeRegions.Any(ourBase => regionsInReach[approachRegion].Contains(ourBase)))
            .ToList();

        var unitGroups = units
            .Where(unit => unit.GetRegion() != null)
            .GroupBy(unit => approachRegions.MinBy(approachRegion => {
                var unitRegion = unit.GetRegion();
                var blockedRegions = regionsOutOfReach[unitRegion];

                return Pathfinder.FindPath(unitRegion, approachRegion, blockedRegions.Except(new [] { approachRegion }).ToHashSet()).GetPathDistance();
            }));

        foreach (var unitGroup in unitGroups) {
            if (unitGroup.Key == null) {
                var stop = 1;
            }

            MoveTowards(unitGroup, unitGroup.Key, regionToEvacuate, regionsOutOfReach, enemyArmy);
        }
    }

    /// <summary>
    /// Moves towards the safe region by following a path that avoids certain regions.
    /// Units will avoid engaging the enemy.
    /// </summary>
    /// <param name="units">The units to move</param>
    /// <param name="regionToEvacuate">The region to go to</param>
    /// <param name="safeRegion">The safe region to get to</param>
    /// <param name="blockedRegions">The regions to avoid going through</param>
    /// <param name="enemyArmy">The enemy units to get in range of but avoid engaging</param>
    private static void MoveTowards(IEnumerable<Unit> units, IRegion safeRegion, IRegion regionToEvacuate, IDictionary<IRegion, HashSet<IRegion>> blockedRegions, IReadOnlyCollection<Unit> enemyArmy) {
        foreach (var unit in units) {
            var unitRegion = unit.GetRegion();
            if (unitRegion == regionToEvacuate) {
                unit.Move(safeRegion.Center);
                continue;
            }

            // TODO GD We do this computation twice per frame, cache it!
            var closestEnemy = enemyArmy.MinBy(enemy => enemy.DistanceTo(unit) - enemy.MaxRange);
            // TODO GD You know what they say about magic numbers!
            if (closestEnemy != null && closestEnemy.DistanceTo(unit) < closestEnemy.MaxRange + 3) {
                unit.MoveAwayFrom(closestEnemy.Position.ToVector2());
                continue;
            }

            if (unitRegion == safeRegion) {
                continue;
            }

            var path = Pathfinder.FindPath(unitRegion, safeRegion, blockedRegions[unitRegion]);
            if (path == null) {
                // Trying to gracefully handle a case that I don't think should happen
                unit.Move(safeRegion.Center);
                continue;
            }

            var nextRegion = path
                .Skip(1)
                .First();

            unit.Move(nextRegion.Center);
        }
    }
}
