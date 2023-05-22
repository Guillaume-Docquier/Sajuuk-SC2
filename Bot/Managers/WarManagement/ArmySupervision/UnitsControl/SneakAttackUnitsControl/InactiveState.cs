using System.Collections.Generic;
using System.Linq;
using Bot.Algorithms;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl.SneakAttackUnitsControl;

public partial class SneakAttack {
    public class InactiveState: SneakAttackState {
        private readonly IUnitsTracker _unitsTracker;
        private readonly ITerrainTracker _terrainTracker;
        private readonly IFrameClock _frameClock;
        private readonly IDetectionTracker _detectionTracker;
        private readonly IUnitEvaluator _unitEvaluator;
        private readonly ISneakAttackStateFactory _sneakAttackStateFactory;

        private const float MinimumEngagementArmyThreshold = 0.75f;
        private const float OverwhelmingForceRatio = 4f;

        private const float OperationRadius = TankRange + 3;

        public InactiveState(
            IUnitsTracker unitsTracker,
            ITerrainTracker terrainTracker,
            IFrameClock frameClock,
            IDetectionTracker detectionTracker,
            IUnitEvaluator unitEvaluator,
            ISneakAttackStateFactory sneakAttackStateFactory
        ) {
            _unitsTracker = unitsTracker;
            _terrainTracker = terrainTracker;
            _frameClock = frameClock;
            _detectionTracker = detectionTracker;
            _unitEvaluator = unitEvaluator;
            _sneakAttackStateFactory = sneakAttackStateFactory;
        }

        public override bool IsViable(IReadOnlyCollection<Unit> army) {
            if (Context._coolDownUntil > _frameClock.CurrentFrame) {
                return false;
            }

            if (_detectionTracker.IsDetected(army)) {
                return false;
            }

            var priorityTargets = Context.GetPriorityTargetsInOperationRadius(army, OperationRadius);
            if (!priorityTargets.Any()) {
                return false;
            }

            var enemiesInSightOfTheArmy = Context.GetGroundEnemiesInSight(army).ToList();
            if (!enemiesInSightOfTheArmy.Any()) {
                // Nobody in sight, nothing to do
                return false;
            }

            if (_unitEvaluator.EvaluateForce(army) >= OverwhelmingForceRatio * _unitEvaluator.EvaluateForce(enemiesInSightOfTheArmy)) {
                // We are very strong, we can overwhelm, no need for this tactic
                return false;
            }

            var enemyMilitaryUnits = _unitsTracker.GetUnits(_unitsTracker.EnemyUnits, Units.Military)
                .OrderBy(enemy => enemy.DistanceTo(_terrainTracker.GetClosestWalkable(army.GetCenter(), searchRadius: 3)))
                .ToList();

            var nbSoldiersInRange = army.Count(soldier => enemyMilitaryUnits.Any(soldier.IsInAttackRangeOf));
            if (nbSoldiersInRange >= army.Count * MinimumEngagementArmyThreshold) {
                // We have a pretty good engagement already
                return false;
            }

            return true;
        }

        protected override void Execute() {
            var closestPriorityTarget = Context.GetPriorityTargetsInOperationRadius(Context._army, OperationRadius).MinBy(enemy => enemy.DistanceTo(Context._armyCenter));
            if (closestPriorityTarget != null) {
                Context._targetPosition = closestPriorityTarget.Position.ToVector2();
                Context._isTargetPriority = true;
            }
            else {
                var closestTarget = Context.GetGroundEnemiesInSight(Context._army).MinBy(enemy => enemy.DistanceTo(Context._armyCenter));
                if (closestTarget == null) {
                    Logger.Error("BurrowSurprise: Went from None -> Fight because no enemies nearby");
                    NextState = _sneakAttackStateFactory.CreateTerminalState();
                    return;
                }

                Context._targetPosition = closestTarget.Position.ToVector2();
                Context._isTargetPriority = false;
            }

            NextState = _sneakAttackStateFactory.CreateApproachState();
        }
    }
}
