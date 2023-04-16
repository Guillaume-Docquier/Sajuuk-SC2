using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameSense.RegionTracking;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.Managers.WarManagement.ArmySupervision.RegionalArmySupervision;

public class DisengageState : RegionalArmySupervisionState {
    private const float SafetyDistance = 5;
    private const float SafetyDistanceTolerance = SafetyDistance / 2;

    private IReadOnlyCollection<Unit> _unitsInSafePosition = new List<Unit>();

    /// <summary>
    /// Evacuate units from the target region and keep rallying other units to a safe position.
    /// Units will only route through safe regions and stay at a safe distance of enemies in the target region.
    /// </summary>
    protected override void Execute() {
        _unitsInSafePosition = GetUnitsInSafePosition(SupervisedUnits, EnemyArmy);
        var unitsInDanger = SupervisedUnits.Except(_unitsInSafePosition).ToList();

        ApproachState.MoveIntoStrikingPosition(_unitsInSafePosition, TargetRegion, EnemyArmy, SafetyDistance + SafetyDistanceTolerance);
        MoveIntoSafePosition(unitsInDanger, EnemyArmy);
    }

    /// <summary>
    /// Transition to ApproachState if all our units are safe.
    /// </summary>
    /// <returns>True if the transition happened, false otherwise</returns>
    protected override bool TryTransitioning() {
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
    private static IReadOnlyCollection<Unit> GetUnitsInSafePosition(IReadOnlyCollection<Unit> supervisedUnits, IReadOnlyCollection<Unit> enemyArmy) {
        if (!enemyArmy.Any()) {
            return supervisedUnits;
        }

        return supervisedUnits
            .Where(unit => RegionTracker.GetForce(unit.GetRegion(), Alliance.Enemy) <= 0)
            .ToList();
    }

    /// <summary>
    /// Moves units out of their current region by targeting the safest exit.
    /// </summary>
    /// <param name="units">The units to move</param>
    /// <param name="enemyArmy">The enemy units to get out of range of</param>
    private static void MoveIntoSafePosition(IReadOnlyCollection<Unit> units, IReadOnlyCollection<Unit> enemyArmy) {
        var unitGroups = units
            .Where(unit => unit.GetRegion() != null)
            .GroupBy(unit => unit.GetRegion()
                .Neighbors
                .Where(neighbor => !neighbor.Region.IsObstructed)
                .MinBy(reachableNeighbor => {
                    var unitDistanceToNeighbor = reachableNeighbor.Frontier.Min(cell => cell.DistanceTo(unit));
                    var enemyDistanceToNeighbor = reachableNeighbor.Frontier.Min(cell => enemyArmy.Min(enemy => cell.DistanceTo(enemy)));
                    var enemyForce = RegionTracker.GetForce(reachableNeighbor.Region, Alliance.Enemy);

                    return unitDistanceToNeighbor / (enemyDistanceToNeighbor + 1) * (enemyForce + 1);
                })
                ?.Region
            );

        foreach (var unitGroup in unitGroups) {
            MoveTowards(unitGroup, unitGroup.Key, enemyArmy);
        }
    }

    /// <summary>
    /// Moves towards the safe region.
    /// Units will avoid engaging the enemy.
    /// </summary>
    /// <param name="units">The units to move</param>
    /// <param name="safeRegion">The safe region to get to</param>
    /// <param name="enemyArmy">The enemy units to get in range of but avoid engaging</param>
    private static void MoveTowards(IEnumerable<Unit> units, IRegion safeRegion, IReadOnlyCollection<Unit> enemyArmy) {
        foreach (var unit in units) {
            // TODO GD Kite away
            unit.Move(safeRegion.Center);
        }
    }
}
