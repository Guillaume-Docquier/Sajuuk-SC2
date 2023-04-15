using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl;
using Bot.MapKnowledge;

namespace Bot.Managers.WarManagement.ArmySupervision.RegionalArmySupervision;

public class EngageState : RegionalArmySupervisionState {
    private IReadOnlySet<Unit> _unitsReadyToAttack = new HashSet<Unit>();

    /// <summary>
    /// Attack the target region with units that are ready to fight.
    /// Other units will route through safe regions to join the fight.
    /// </summary>
    protected override void Execute() {
        _unitsReadyToAttack = GetUnitsReadyToAttack(SupervisedUnits, TargetRegion, EnemyArmy);

        Attack(_unitsReadyToAttack, TargetRegion, EnemyArmy, UnitsController);
        JoinTheFight(SupervisedUnits.Except(_unitsReadyToAttack).ToList(), TargetRegion, EnemyArmy);
    }

    /// <summary>
    /// Transition to DisengageState if we've suffered too many losses or if the enemy army is bigger than expected.
    /// </summary>
    /// <returns>True if the transition happened, false otherwise</returns>
    protected override bool TryTransitioning() {
        // TODO GD We should consider if retreating is even possible
        // TODO GD Sometimes you have to commit
        if (_unitsReadyToAttack.GetForce() < EnemyArmy.GetForce() * 0.75) {
            StateMachine.TransitionTo(new ApproachState());
            return true;
        }

        return false;
    }

    /// <summary>
    /// All units that are not fighting are free to go
    /// </summary>
    /// <returns>The units that can be released</returns>
    public override IEnumerable<Unit> GetReleasableUnits() {
        return EnemyArmy.GetForce() == 0
            ? SupervisedUnits
            : SupervisedUnits.Except(_unitsReadyToAttack);
    }

    /// <summary>
    /// Gets all the units that are in position and ready to attack the target region.
    /// </summary>
    /// <param name="supervisedUnits">The units to consider</param>
    /// <param name="targetRegion">The region to strike</param>
    /// <param name="enemyArmy">The enemy army to strike</param>
    /// <returns>The units that are ready to attack</returns>
    private static HashSet<Unit> GetUnitsReadyToAttack(IEnumerable<Unit> supervisedUnits, IRegion targetRegion, IReadOnlyCollection<Unit> enemyArmy) {
        return supervisedUnits
            .Where(unit => {
                if (unit.GetRegion() == targetRegion) {
                    return true;
                }

                var closestEnemy = enemyArmy.MinBy(enemy => enemy.DistanceTo(unit) - enemy.MaxRange);

                return closestEnemy != null && closestEnemy.DistanceTo(unit) < closestEnemy.MaxRange + ApproachState.SafetyDistance + ApproachState.SafetyDistanceTolerance;
            })
            .ToHashSet();
    }

    /// <summary>
    /// Attacks the target region.
    /// </summary>
    /// <param name="units">The units that must attack</param>
    /// <param name="targetRegion">The region to attack</param>
    /// <param name="enemyArmy">The enemy units to engage</param>
    /// <param name="unitsController">The units controller</param>
    private static void Attack(IReadOnlySet<Unit> units, IRegion targetRegion, IReadOnlyCollection<Unit> enemyArmy, IUnitsControl unitsController) {
        // TODO GD We can improve target selection
        var target = targetRegion.Center;
        if (enemyArmy.Any()) {
            target = enemyArmy.MinBy(unit => unit.DistanceTo(targetRegion.Center)).Position.ToVector2();
        }

        var unhandledUnits = unitsController.Execute(units);
        foreach (var unhandledUnit in unhandledUnits) {
            unhandledUnit.AttackMove(target);
        }
    }

    /// <summary>
    /// Moves units into strike range by using the given approach regions.
    /// Units will avoid engaging the enemy.
    /// </summary>
    /// <param name="units">The units to move</param>
    /// <param name="targetRegion">The region to go to</param>
    /// <param name="enemyArmy">The enemy units to get in range of but avoid engaging</param>
    private static void JoinTheFight(IReadOnlyCollection<Unit> units, IRegion targetRegion, IReadOnlyCollection<Unit> enemyArmy) {
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
    /// Moves towards the target region by following a path that avoids certain regions.
    /// Units will avoid engaging the enemy.
    /// </summary>
    /// <param name="units">The units to move</param>
    /// <param name="targetRegion">The region to go to</param>
    /// <param name="approachRegion">The region to go though to get to the target region</param>
    /// <param name="blockedRegions">The regions to avoid going through</param>
    /// <param name="enemyArmy">The enemy units to get in range of but avoid engaging</param>
    private static void MoveTowards(IEnumerable<Unit> units, IRegion targetRegion, IRegion approachRegion, IDictionary<IRegion, HashSet<IRegion>> blockedRegions, IReadOnlyCollection<Unit> enemyArmy) {
        foreach (var unit in units) {
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
