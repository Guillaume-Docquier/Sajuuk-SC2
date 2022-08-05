using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.UnitModules;

namespace Bot.Managers.ArmyManagement.Tactics.SneakAttack;

public partial class SneakAttackTactic {
    public class InactiveState: SneakAttackState {
        private bool _goToNextState = false;
        private bool _goToExitState = false;

        public override bool IsViable(IReadOnlyCollection<Unit> army) {
            if (StateMachine._coolDownUntil > Controller.Frame) {
                return false;
            }

            if (!HasProperTech()) {
                return false;
            }

            if (DetectionTracker.IsDetected(army)) {
                return false;
            }

            var armyCenter = army.GetCenter();
            var maxSightRange = army.DistinctBy(soldier => soldier.UnitType)
                .Select(soldier => soldier.UnitTypeData.SightRange)
                .Max();

            var enemiesInSight = GetGroundEnemiesInSight(army).ToList();
            if (!enemiesInSight.Any()) {
                return false;
            }

            return enemiesInSight.MinBy(enemy => enemy.HorizontalDistanceTo(armyCenter))!.HorizontalDistanceTo(armyCenter) >= maxSightRange / 2;
        }

        protected override bool TryTransitioning() {
            if (_goToNextState) {
                StateMachine.TransitionTo(new ApproachState());
                return true;
            }

            if (_goToExitState) {
                StateMachine.TransitionTo(new FightState());
                return true;
            }

            return false;
        }

        protected override void Execute() {
            foreach (var roach in StateMachine._army.Where(roach => !StateMachine._unitsWithUninstalledModule.Contains(roach))) {
                roach.AddDeathWatcher(StateMachine); // That's wierd
                StateMachine._unitsWithUninstalledModule.Add(roach);
                UnitModule.Uninstall<BurrowMicroModule>(roach);
            }

            BurrowOverlings(StateMachine._army);

            var closestPriorityTarget = GetPriorityTargetsInOperationRadius(StateMachine._armyCenter).MinBy(enemy => enemy.HorizontalDistanceTo(StateMachine._armyCenter));
            if (closestPriorityTarget != null) {
                StateMachine._targetPosition = closestPriorityTarget.Position;
                StateMachine._isTargetPriority = true;
            }
            else {
                var closestEnemy = GetGroundEnemiesInSight(StateMachine._army).MinBy(enemy => enemy.HorizontalDistanceTo(StateMachine._armyCenter));
                if (closestEnemy == null) {
                    Logger.Error("BurrowSurprise: Went from None -> Fight because no enemies nearby");
                    _goToExitState = true;
                    return;
                }

                StateMachine._targetPosition = closestEnemy.Position;
            }

            _goToNextState = true;
        }
    }
}
