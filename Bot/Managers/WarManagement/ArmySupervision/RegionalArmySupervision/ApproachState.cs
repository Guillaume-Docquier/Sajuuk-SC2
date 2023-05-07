using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.GameSense.RegionsEvaluationsTracking;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl;
using Bot.MapAnalysis;
using Bot.MapAnalysis.RegionAnalysis;

namespace Bot.Managers.WarManagement.ArmySupervision.RegionalArmySupervision;

public class ApproachState : RegionalArmySupervisionState {
    private readonly IRegionsTracker _regionsTracker;
    private readonly IRegionsEvaluationsTracker _regionsEvaluationsTracker;
    private readonly IRegionalArmySupervisorStateFactory _regionalArmySupervisorStateFactory;

    public const float SafetyDistance = 5;
    public const float SafetyDistanceTolerance = SafetyDistance / 2;

    public ApproachState(
        IRegionsTracker regionsTracker,
        IRegionsEvaluationsTracker regionsEvaluationsTracker,
        IRegionalArmySupervisorStateFactory regionalArmySupervisorStateFactory
    ) {
        _regionsTracker = regionsTracker;
        _regionsEvaluationsTracker = regionsEvaluationsTracker;
        _regionalArmySupervisorStateFactory = regionalArmySupervisorStateFactory;
    }

    /// <summary>
    /// Move all units into striking position to prepare for an assault.
    /// Units will only route through safe regions and stay at a safe distance of enemies in the target region.
    /// </summary>
    protected override void Execute() {
        MoveIntoStrikingPosition(SupervisedUnits, TargetRegion, EnemyArmy, SafetyDistance, DefensiveUnitsController, _regionsTracker.Regions.ToHashSet(), _regionsEvaluationsTracker);
    }

    /// <summary>
    /// Transition to EngageState once enough units are in striking position.
    /// </summary>
    /// <returns>True if the transition happened, false otherwise</returns>
    protected override bool TryTransitioning() {
        var unitsInStrikingPosition = GetUnitsInStrikingPosition(SupervisedUnits, TargetRegion, EnemyArmy);
        // TODO GD If maxed out, we have to trade
        if (unitsInStrikingPosition.GetForce() >= EnemyArmy.GetForce() * 1.25) {
            StateMachine.TransitionTo(_regionalArmySupervisorStateFactory.CreateEngageState());
            return true;
        }

        return false;
    }

    /// <summary>
    /// All units are free to go
    /// </summary>
    /// <returns>The units that can be released</returns>
    public override IEnumerable<Unit> GetReleasableUnits() {
        return SupervisedUnits;
    }

    public override void Release(Unit unit) {
        // Nothing to do
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

                return closestEnemy != null && closestEnemy.DistanceTo(unit) < closestEnemy.MaxRange + SafetyDistance + SafetyDistanceTolerance;
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
    /// <param name="safetyDistance">A safety distance to keep between the enemy</param>
    /// <param name="unitsController">The units controller</param>
    /// <param name="allRegions">All the regions</param>
    /// <param name="regionsEvaluationsTracker">TODO GD Review this parameter</param>
    public static void MoveIntoStrikingPosition(
        IReadOnlyCollection<Unit> units,
        IRegion targetRegion,
        IReadOnlyCollection<Unit> enemyArmy,
        float safetyDistance,
        IUnitsControl unitsController,
        // TODO GD This signature is crap, but I want MoveIntoStrikingPosition to be shareable
        // TODO GD I should use composition, but right now it's hard to see properly
        IReadOnlySet<IRegion> allRegions,
        IRegionsEvaluationsTracker regionsEvaluationsTracker
    ) {
        var approachRegions = targetRegion.GetReachableNeighbors().ToList();

        var regionsWithFriendlyUnitPresence = units
            .Select(unit => unit.GetRegion())
            .Where(region => region != null)
            .ToHashSet();

        var regionsOutOfReach = ComputeBlockedRegionsMap(regionsWithFriendlyUnitPresence, allRegions, regionsEvaluationsTracker);

        var unitGroups = units
            .Where(unit => unit.GetRegion() != null)
            .GroupBy(unit => approachRegions.MinBy(approachRegion => {
                var unitRegion = unit.GetRegion();
                var blockedRegions = regionsOutOfReach[unitRegion];
                if (blockedRegions.Contains(approachRegion)) {
                    return float.MaxValue;
                }

                return Pathfinder.Instance.FindPath(unitRegion, approachRegion, blockedRegions).GetPathDistance();
            }));

        foreach (var unitGroup in unitGroups) {
            MoveTowards(unitGroup, targetRegion, unitGroup.Key, regionsOutOfReach, enemyArmy, safetyDistance, unitsController);
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
    /// <param name="safetyDistance">A safety distance to keep between the enemy</param>
    /// <param name="unitsController">The units controller</param>
    private static void MoveTowards(
        IEnumerable<Unit> units,
        IRegion targetRegion,
        IRegion approachRegion,
        IDictionary<IRegion, HashSet<IRegion>> blockedRegions,
        IReadOnlyCollection<Unit> enemyArmy,
        float safetyDistance,
        IUnitsControl unitsController
    ) {
        var uncontrolledUnits = unitsController.Execute(units.ToHashSet());
        foreach (var unit in uncontrolledUnits) {
            // TODO GD We do this computation twice per frame, cache it!
            var closestEnemy = enemyArmy.MinBy(enemy => enemy.DistanceTo(unit) - enemy.MaxRange);
            if (closestEnemy != null && closestEnemy.DistanceTo(unit) < closestEnemy.MaxRange + safetyDistance) {
                // TODO GD We should avoid cornering ourselves, maybe we should go towards a region exit?
                unit.MoveAwayFrom(closestEnemy.Position.ToVector2());
                continue;
            }

            var unitRegion = unit.GetRegion();

            if (unitRegion == approachRegion) {
                unit.Move(targetRegion.Center);
                continue;
            }

            var path = Pathfinder.Instance.FindPath(unitRegion, approachRegion, blockedRegions[unitRegion]);
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
