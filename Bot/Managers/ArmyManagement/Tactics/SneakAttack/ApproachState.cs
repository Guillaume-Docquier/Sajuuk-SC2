using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameSense;

namespace Bot.Managers.ArmyManagement.Tactics.SneakAttack;

public partial class SneakAttackTactic {
    public class ApproachState : SneakAttackState {
        private const float SetupDistance = 1.25f;

        private bool _goToSetupState = false;
        private bool _goToExitState = false;

        public override bool IsViable(IReadOnlyCollection<Unit> army) {
            if (DetectionTracker.IsDetected(army)) {
                return false;
            }

            // If we're engaged, it means they somehow see us, abort!
            return !IsArmyGettingEngaged(army);
        }

        protected override bool TryTransitioning() {
            if (_goToSetupState) {
                StateMachine.TransitionTo(new SetupState());
                return true;
            }

            if (_goToExitState) {
                StateMachine.TransitionTo(new FightState());
                return true;
            }

            return false;
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
                Logger.Warning("BurrowSurprise: Went from Approach -> Fight because _targetPosition == default");
                StateMachine._isTargetPriority = false;
                _goToExitState = true;
                return;
            }

            if (StateMachine._targetPosition.HorizontalDistanceTo(StateMachine._armyCenter) > SetupDistance) {
                BurrowOverlings(StateMachine._army);

                foreach (var soldier in StateMachine._army.Where(soldier => soldier.IsIdleOrMovingOrAttacking())) {
                    soldier.Move(StateMachine._targetPosition);
                }
            }
            else {
                StateMachine._targetPosition = default;
                StateMachine._isTargetPriority = false;
                _goToSetupState = true;
            }
        }
    }
}
