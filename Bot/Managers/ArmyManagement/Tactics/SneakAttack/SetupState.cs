using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;

namespace Bot.Managers.ArmyManagement.Tactics.SneakAttack;

public partial class SneakAttackTactic {
    public class SetupState : SneakAttackState {
        private const float EngageDistance = 0.75f;
        private const float MinimumEngagementArmyThreshold = 0.75f;

        public override bool IsViable(IReadOnlyCollection<Unit> army) {
            if (DetectionTracker.IsDetected(army)) {
                return false;
            }

            // If we're engaged, it means they somehow see us, abort!
            return !IsArmyGettingEngaged(army);
        }

        protected override void OnSetStateMachine() {
            StateMachine._targetPosition = default;
            StateMachine._isTargetPriority = false;
        }

        protected override void Execute() {
            // Do we need _isTargetPriority at this point? We shouldn't lose sight at this point, right?
            var closestPriorityTarget = GetPriorityTargetsInOperationRadius(StateMachine._armyCenter).MinBy(enemy => enemy.HorizontalDistanceTo(StateMachine._armyCenter));
            if (closestPriorityTarget != null) {
                StateMachine._targetPosition = closestPriorityTarget.Position;
                StateMachine._isTargetPriority = true;
            }
            else {
                var enemies = Controller.GetUnits(UnitsTracker.EnemyUnits, Units.Military).ToList();
                var closestEnemyCluster = Clustering.DBSCAN(enemies, 5, 2).MinBy(cluster => cluster.GetCenter().HorizontalDistanceTo(StateMachine._armyCenter));

                // TODO GD Tweak this to put most of our army in range of the cluster instead
                if (closestEnemyCluster != null && StateMachine._armyCenter.HorizontalDistanceTo(closestEnemyCluster.GetCenter()) <= OperationRadius) {
                    StateMachine._targetPosition = closestEnemyCluster.GetCenter();
                    StateMachine._isTargetPriority = false;
                }
            }

            if (StateMachine._targetPosition == default) {
                Logger.Warning("{0}: {1} has no target", StateMachine.GetType().Name, GetType().Name);
                NextState = new TerminalState();
                StateMachine._isTargetPriority = false;

                return;
            }

            BurrowOverlings(StateMachine._army);

            if (StateMachine._targetPosition.HorizontalDistanceTo(StateMachine._armyCenter) > EngageDistance) {
                foreach (var soldier in StateMachine._army.Where(soldier => soldier.IsIdleOrMovingOrAttacking())) {
                    soldier.Move(StateMachine._targetPosition);
                }
            }
            else if (GetArmyWithEnoughHealth(StateMachine._army).Count() >= StateMachine._army.Count * MinimumEngagementArmyThreshold) {
                NextState = new EngageState();
            }
        }
    }
}
