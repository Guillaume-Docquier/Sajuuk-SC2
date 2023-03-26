using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl.SneakAttack;

public partial class SneakAttackUnitsControl {
    public class InactiveState: SneakAttackState {
        private const float MinimumEngagementArmyThreshold = 0.75f;
        private const float OverwhelmingForceRatio = 4f;

        public override bool IsViable(IReadOnlyCollection<Unit> army) {
            if (Context._coolDownUntil > Controller.Frame) {
                return false;
            }

            if (DetectionTracker.IsDetected(army)) {
                return false;
            }

            // TODO GD Should probably weight this in when doing the other checks instead of returning true
            var priorityTargets = GetPriorityTargetsInOperationRadius(army.GetCenter());
            if (priorityTargets.Any()) {
                // F*ck em up!
                return true;
            }

            var enemiesInSightOfTheArmy = GetGroundEnemiesInSight(army).ToList();
            if (!enemiesInSightOfTheArmy.Any()) {
                // Nobody in sight, nothing to do
                return false;
            }

            if (army.GetForce() >= OverwhelmingForceRatio * enemiesInSightOfTheArmy.GetForce()) {
                // We are very strong, we can overwhelm, no need for this tactic
                return false;
            }

            var enemyMilitaryUnits = Controller.GetUnits(UnitsTracker.EnemyUnits, Units.Military)
                .OrderBy(enemy => enemy.DistanceTo(army.GetCenter()))
                .ToList();

            var nbSoldiersInRange = army.Count(soldier => enemyMilitaryUnits.Any(soldier.IsInAttackRangeOf));

            if (nbSoldiersInRange >= army.Count * MinimumEngagementArmyThreshold) {
                // We have a pretty good engagement already
                return false;
            }

            return true;
        }

        protected override void Execute() {
            var closestPriorityTarget = GetPriorityTargetsInOperationRadius(Context._armyCenter).MinBy(enemy => enemy.DistanceTo(Context._armyCenter));
            if (closestPriorityTarget != null) {
                Context._targetPosition = closestPriorityTarget.Position.ToVector2();
                Context._isTargetPriority = true;
            }
            else {
                var closestTarget = GetGroundEnemiesInSight(Context._army).MinBy(enemy => enemy.DistanceTo(Context._armyCenter));
                if (closestTarget == null) {
                    Logger.Error("BurrowSurprise: Went from None -> Fight because no enemies nearby");
                    NextState = new TerminalState();
                    return;
                }

                Context._targetPosition = closestTarget.Position.ToVector2();
                Context._isTargetPriority = false;
            }

            NextState = new ApproachState();
        }
    }
}
