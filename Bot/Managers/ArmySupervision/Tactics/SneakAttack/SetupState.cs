using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.UnitModules;

namespace Bot.Managers.ArmySupervision.Tactics.SneakAttack;

public partial class SneakAttackTactic {
    public class SetupState : SneakAttackState {
        private const float EngageDistance = 1f;
        private const float MinimumEngagementArmyThreshold = 0.75f;

        private readonly StuckDetector _stuckDetector = new StuckDetector();

        public override bool IsViable(IReadOnlyCollection<Unit> army) {
            if (DetectionTracker.IsDetected(army)) {
                return false;
            }

            // If we're engaged, it means they somehow see us, abort!
            return !IsArmyGettingEngaged(army);
        }

        protected override void OnSetStateMachine() {
            StateMachine.Context._targetPosition = default;
            StateMachine.Context._isTargetPriority = false;
        }

        protected override void Execute() {
            _stuckDetector.Tick(StateMachine.Context._armyCenter);
            if (_stuckDetector.IsStuck) {
                Logger.Warning("{0} army is stuck", Name);
                NextState = new TerminalState();

                return;
            }

            ComputeTargetPosition();

            if (StateMachine.Context._targetPosition == default) {
                Logger.Warning("{0} has no target", Name);
                NextState = new TerminalState();
                StateMachine.Context._isTargetPriority = false;

                return;
            }

            BurrowOverlings(StateMachine.Context._army);

            if (IsReadyToEngage()) {
                NextState = new EngageState();
            }
            else {
                MoveArmyIntoPosition();
            }
        }

        private void ComputeTargetPosition() {
            // Do we need _isTargetPriority at this point? We shouldn't lose sight at this point, right?
            var closestPriorityTarget = GetPriorityTargetsInOperationRadius(StateMachine.Context._armyCenter)
                .MinBy(enemy => enemy.HorizontalDistanceTo(StateMachine.Context._armyCenter));

            if (closestPriorityTarget != null) {
                StateMachine.Context._targetPosition = closestPriorityTarget.Position;
                StateMachine.Context._isTargetPriority = true;
            }
            else {
                var enemies = Controller.GetUnits(UnitsTracker.EnemyUnits, Units.Military).ToList();
                var closestEnemyCluster = Clustering.DBSCAN(enemies, 5, 2).clusters.MinBy(cluster => cluster.GetCenter().HorizontalDistanceTo(StateMachine.Context._armyCenter));

                // TODO GD Tweak this to create a concave instead?
                if (closestEnemyCluster != null && StateMachine.Context._armyCenter.HorizontalDistanceTo(closestEnemyCluster.GetCenter()) <= OperationRadius) {
                    StateMachine.Context._targetPosition = closestEnemyCluster.GetCenter();
                    StateMachine.Context._isTargetPriority = false;
                }
            }
        }

        private bool IsReadyToEngage() {
            return HasEnoughArmyInRange() && IsArmyHealthyEnough();
        }

        private bool HasEnoughArmyInRange() {
            if (StateMachine.Context._targetPosition.HorizontalDistanceTo(StateMachine.Context._armyCenter) <= EngageDistance) {
                return true;
            }

            // TODO GD We should know what the priority target is and check if we're in range of it
            if (StateMachine.Context._isTargetPriority) {
                return false;
            }

            var enemyMilitaryUnits = Controller.GetUnits(UnitsTracker.EnemyUnits, Units.Military)
                .OrderBy(enemy => enemy.HorizontalDistanceTo(StateMachine.Context._armyCenter))
                .ToList();

            var nbSoldiersInRange = StateMachine.Context._army.Count(soldier => enemyMilitaryUnits.Any(soldier.IsInRangeOf));

            return nbSoldiersInRange >= StateMachine.Context._army.Count * MinimumEngagementArmyThreshold;
        }

        private bool IsArmyHealthyEnough() {
            var armyWithEnoughHealth = StateMachine.Context._army.Where(soldier => soldier.Integrity > BurrowMicroModule.BurrowDownThreshold);

            return armyWithEnoughHealth.Count() >= StateMachine.Context._army.Count * MinimumEngagementArmyThreshold;
        }

        private void MoveArmyIntoPosition() {
            // TODO GD Have the unit decide if it can receive move/attack orders instead of manually checking
            // The use case here is to not prevent burrow/unburrow
            foreach (var soldier in StateMachine.Context._army.Where(soldier => soldier.IsIdleOrMovingOrAttacking())) {
                soldier.Move(StateMachine.Context._targetPosition);
            }
        }
    }
}
