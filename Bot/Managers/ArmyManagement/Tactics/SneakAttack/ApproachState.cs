using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameSense;

namespace Bot.Managers.ArmyManagement.Tactics.SneakAttack;

public partial class SneakAttackTactic {
    public class ApproachState : SneakAttackState {
        private const float SetupDistance = 1.25f;

        public override bool IsViable(IReadOnlyCollection<Unit> army) {
            if (DetectionTracker.IsDetected(army)) {
                return false;
            }

            // If we're engaged, it means they somehow see us, abort!
            return !IsArmyGettingEngaged(army);
        }

        protected override void Execute() {
            var closestPriorityTarget = GetPriorityTargetsInOperationRadius(StateMachine._armyCenter).MinBy(enemy => enemy.HorizontalDistanceTo(StateMachine._armyCenter));
            if (closestPriorityTarget != null) {
                StateMachine._targetPosition = closestPriorityTarget.Position;
                StateMachine._isTargetPriority = true;
            }
            else if (!StateMachine._isTargetPriority) {
                var closestVisibleEnemy = GetGroundEnemiesInSight(StateMachine._army).MinBy(enemy => enemy.HorizontalDistanceTo(StateMachine._armyCenter));
                if (closestVisibleEnemy != null) {
                    StateMachine._targetPosition = closestVisibleEnemy.Position;
                    StateMachine._isTargetPriority = false;
                }
            }

            if (StateMachine._targetPosition == default) {
                Logger.Warning("{0}: {1} has no target", StateMachine.GetType().Name, GetType().Name);
                StateMachine._isTargetPriority = false;
                NextState = new TerminalState();

                return;
            }

            BurrowOverlings(StateMachine._army);

            if (StateMachine._targetPosition.HorizontalDistanceTo(StateMachine._armyCenter) > SetupDistance) {
                foreach (var soldier in StateMachine._army.Where(soldier => soldier.IsIdleOrMovingOrAttacking())) {
                    soldier.Move(StateMachine._targetPosition);
                }
            }
            else {
                NextState = new SetupState();
            }
        }
    }
}
