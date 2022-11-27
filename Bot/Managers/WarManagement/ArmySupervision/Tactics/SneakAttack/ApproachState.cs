using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameSense;

namespace Bot.Managers.WarManagement.ArmySupervision.Tactics.SneakAttack;

public partial class SneakAttackTactic {
    public class ApproachState : SneakAttackState {
        private const float SetupDistance = 1.25f;

        private readonly StuckDetector _stuckDetector = new StuckDetector();

        public override bool IsViable(IReadOnlyCollection<Unit> army) {
            if (DetectionTracker.IsDetected(army)) {
                return false;
            }

            // If we're engaged, it means they somehow see us, abort!
            return !IsArmyGettingEngaged(army);
        }

        protected override void Execute() {
            _stuckDetector.Tick(Context._armyCenter);
            if (_stuckDetector.IsStuck) {
                Logger.Warning("{0} army is stuck", Name);
                NextState = new TerminalState();

                return;
            }

            var closestPriorityTarget = GetPriorityTargetsInOperationRadius(Context._armyCenter).MinBy(enemy => enemy.DistanceTo(Context._armyCenter));
            if (closestPriorityTarget != null) {
                Context._targetPosition = closestPriorityTarget.Position.ToVector2();
                Context._isTargetPriority = true;
            }
            else if (!Context._isTargetPriority) {
                var closestVisibleEnemy = GetGroundEnemiesInSight(Context._army).MinBy(enemy => enemy.DistanceTo(Context._armyCenter));
                if (closestVisibleEnemy != null) {
                    Context._targetPosition = closestVisibleEnemy.Position.ToVector2();
                    Context._isTargetPriority = false;
                }
            }

            if (Context._targetPosition == default) {
                Logger.Warning("{0} has no target", Name);
                Context._isTargetPriority = false;
                NextState = new TerminalState();

                return;
            }

            BurrowOverlings(Context._army);

            if (Context._targetPosition.DistanceTo(Context._armyCenter) > SetupDistance) {
                foreach (var soldier in Context._army.Where(soldier => soldier.IsIdleOrMovingOrAttacking())) {
                    soldier.Move(Context._targetPosition);
                }
            }
            else {
                NextState = new SetupState();
            }
        }
    }
}
