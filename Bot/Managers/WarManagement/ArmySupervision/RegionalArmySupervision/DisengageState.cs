using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.GameSense.RegionTracking;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl;
using Bot.MapAnalysis.RegionAnalysis;
using SC2APIProtocol;

namespace Bot.Managers.WarManagement.ArmySupervision.RegionalArmySupervision;

public class DisengageState : RegionalArmySupervisionState {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly IRegionsEvaluationsTracker _regionsEvaluationsTracker;

    private const float SafetyDistance = 5;
    private const float SafetyDistanceTolerance = SafetyDistance / 2;

    private readonly IUnitsControl _fleeKiting;

    private HashSet<Unit> _unitsInSafePosition = new HashSet<Unit>();

    public DisengageState(IUnitsTracker unitsTracker, IRegionsTracker regionsTracker, IRegionsEvaluationsTracker regionsEvaluationsTracker) {
        _unitsTracker = unitsTracker;
        _regionsTracker = regionsTracker;
        _regionsEvaluationsTracker = regionsEvaluationsTracker;

        _fleeKiting = new DisengagementKiting(_unitsTracker);
    }

    /// <summary>
    /// Evacuate units from the target region and keep rallying other units to a safe position.
    /// Units will only route through safe regions and stay at a safe distance of enemies in the target region.
    /// </summary>
    protected override void Execute() {
        _unitsInSafePosition = GetUnitsInSafePosition(SupervisedUnits, EnemyArmy);
        var unitsInDanger = SupervisedUnits.Except(_unitsInSafePosition).ToList();

        ApproachState.MoveIntoStrikingPosition(_unitsInSafePosition, TargetRegion, EnemyArmy, SafetyDistance + SafetyDistanceTolerance, DefensiveUnitsController, _regionsTracker.Regions.ToHashSet(), _regionsEvaluationsTracker);
        MoveIntoSafePosition(unitsInDanger, EnemyArmy, _fleeKiting);
    }

    /// <summary>
    /// Transition to ApproachState if all our units are safe.
    /// </summary>
    /// <returns>True if the transition happened, false otherwise</returns>
    protected override bool TryTransitioning() {
        if (_unitsInSafePosition.Count < SupervisedUnits.Count) {
            return false;
        }

        StateMachine.TransitionTo(new ApproachState(_unitsTracker, _regionsTracker, _regionsEvaluationsTracker));
        return true;
    }

    /// <summary>
    /// All safe units are free to go
    /// </summary>
    /// <returns>The units that can be released</returns>
    public override IEnumerable<Unit> GetReleasableUnits() {
        return _unitsInSafePosition;
    }

    public override void Release(Unit unit) {
        _unitsInSafePosition.Remove(unit);
    }

    /// <summary>
    /// Gets all the units that are in a safe position
    /// </summary>
    /// <param name="supervisedUnits">The units to consider</param>
    /// <param name="enemyArmy">The enemy army to strike</param>
    /// <returns>The units that are in a safe position</returns>
    private HashSet<Unit> GetUnitsInSafePosition(HashSet<Unit> supervisedUnits, IReadOnlyCollection<Unit> enemyArmy) {
        if (!enemyArmy.Any()) {
            return supervisedUnits;
        }

        return supervisedUnits
            .Where(unit => _regionsEvaluationsTracker.GetForce(unit.GetRegion(), Alliance.Enemy) <= 0)
            .ToHashSet();
    }

    /// <summary>
    /// Moves units out of their current region by targeting the safest exit.
    /// </summary>
    /// <param name="units">The units to move</param>
    /// <param name="enemyArmy">The enemy units to get out of range of</param>
    /// <param name="unitsController">The units controller</param>
    private void MoveIntoSafePosition(IReadOnlyCollection<Unit> units, IReadOnlyCollection<Unit> enemyArmy, IUnitsControl unitsController) {
        var unitGroups = units
            .Where(unit => unit.GetRegion() != null)
            .GroupBy(unit => unit.GetRegion()
                .Neighbors
                .Where(neighbor => !neighbor.Region.IsObstructed)
                .MinBy(reachableNeighbor => {
                    var unitDistanceToNeighbor = reachableNeighbor.Frontier.Min(cell => cell.DistanceTo(unit));
                    var enemyDistanceToNeighbor = reachableNeighbor.Frontier.Min(cell => enemyArmy.Min(enemy => cell.DistanceTo(enemy)));
                    var enemyForce = _regionsEvaluationsTracker.GetForce(reachableNeighbor.Region, Alliance.Enemy);

                    return unitDistanceToNeighbor / (enemyDistanceToNeighbor + 1) * (enemyForce + 1);
                })
                ?.Region
            );

        foreach (var unitGroup in unitGroups) {
            MoveTowards(unitGroup, unitGroup.Key, unitsController);
        }
    }

    /// <summary>
    /// Moves towards the safe region.
    /// Units will avoid engaging the enemy.
    /// </summary>
    /// <param name="units">The units to move</param>
    /// <param name="safeRegion">The safe region to get to</param>
    /// <param name="unitsController">The units controller</param>
    private static void MoveTowards(IEnumerable<Unit> units, IRegion safeRegion, IUnitsControl unitsController) {
        var uncontrolledUnits = unitsController.Execute(units.ToHashSet());
        foreach (var unit in uncontrolledUnits) {
            unit.Move(safeRegion.Center);
        }
    }
}
