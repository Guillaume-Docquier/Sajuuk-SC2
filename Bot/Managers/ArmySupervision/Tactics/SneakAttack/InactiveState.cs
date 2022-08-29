using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameSense;

namespace Bot.Managers.ArmySupervision.Tactics.SneakAttack;

public partial class SneakAttackTactic {
    public class InactiveState: SneakAttackState {
        public override bool IsViable(IReadOnlyCollection<Unit> army) {
            if (StateMachine.Context._coolDownUntil > Controller.Frame) {
                return false;
            }

            if (DetectionTracker.IsDetected(army)) {
                return false;
            }

            var armyCenter = army.GetCenter();
            var maxSightRange = army.DistinctBy(soldier => soldier.UnitType)
                .Select(soldier => soldier.UnitTypeData.SightRange)
                .Max();

            var enemiesInSightOfTheArmy = GetGroundEnemiesInSight(army).ToList();
            if (!enemiesInSightOfTheArmy.Any()) {
                return false;
            }

            return enemiesInSightOfTheArmy.All(enemy => enemy.HorizontalDistanceTo(armyCenter) >= maxSightRange / 2);
        }

        protected override void Execute() {
            var closestPriorityTarget = GetPriorityTargetsInOperationRadius(StateMachine.Context._armyCenter).MinBy(enemy => enemy.HorizontalDistanceTo(StateMachine.Context._armyCenter));
            if (closestPriorityTarget != null) {
                StateMachine.Context._targetPosition = closestPriorityTarget.Position;
                StateMachine.Context._isTargetPriority = true;
            }
            else {
                var closestTarget = GetGroundEnemiesInSight(StateMachine.Context._army).MinBy(enemy => enemy.HorizontalDistanceTo(StateMachine.Context._armyCenter));
                if (closestTarget == null) {
                    Logger.Error("BurrowSurprise: Went from None -> Fight because no enemies nearby");
                    NextState = new TerminalState();
                    return;
                }

                StateMachine.Context._targetPosition = closestTarget.Position;
                StateMachine.Context._isTargetPriority = false;
            }

            NextState = new ApproachState();
        }
    }
}
