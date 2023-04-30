using System.Collections.Generic;
using System.Linq;
using Bot.Algorithms;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl.SneakAttackUnitsControl;

public partial class SneakAttack {
    public class SetupState : SneakAttackState {
        private readonly IUnitsTracker _unitsTracker;
        private readonly ITerrainTracker _terrainTracker;

        private const float EngageDistance = 1f;
        private const float MinimumArmyThresholdToEngage = 0.80f;
        private const double MinimumIntegrityToEngage = 0.6;

        private const float OperationRadius = TankRange - 5;

        private readonly StuckDetector _stuckDetector = new StuckDetector();

        public SetupState(IUnitsTracker unitsTracker, ITerrainTracker terrainTracker) {
            _unitsTracker = unitsTracker;
            _terrainTracker = terrainTracker;
        }

        public override bool IsViable(IReadOnlyCollection<Unit> army) {
            if (DetectionTracker.Instance.IsDetected(army)) {
                return false;
            }

            var priorityTargets = Context.GetPriorityTargetsInOperationRadius(army, OperationRadius);
            if (!priorityTargets.Any()) {
                return false;
            }

            // If we're engaged, it means they somehow see us, abort!
            return !IsArmyGettingEngaged(army);
        }

        protected override void OnContextSet() {
            Context._targetPosition = default;
            Context._isTargetPriority = false;
        }

        protected override void Execute() {
            _stuckDetector.Tick(Context._armyCenter);
            if (_stuckDetector.IsStuck) {
                Logger.Warning("{0} army is stuck", Name);
                NextState = new TerminalState(_unitsTracker, _terrainTracker);

                return;
            }

            ComputeTargetPosition();

            if (Context._targetPosition == default) {
                Logger.Warning("{0} has no target", Name);
                NextState = new TerminalState(_unitsTracker, _terrainTracker);
                Context._isTargetPriority = false;

                return;
            }

            if (IsReadyToEngage()) {
                NextState = new EngageState(_unitsTracker, _terrainTracker);
            }
            else {
                MoveArmyIntoPosition();
            }
        }

        private void ComputeTargetPosition() {
            // Do we need _isTargetPriority at this point? We shouldn't lose sight at this point, right?
            var closestPriorityTarget = Context.GetPriorityTargetsInOperationRadius(Context._army, OperationRadius)
                .MinBy(enemy => enemy.DistanceTo(Context._armyCenter));

            if (closestPriorityTarget != null) {
                Context._targetPosition = closestPriorityTarget.Position.ToVector2();
                Context._isTargetPriority = true;
            }
            else {
                var enemies = Controller.GetUnits(_unitsTracker.EnemyUnits, Units.Military).ToList();
                var closestEnemyCluster = Clustering.Instance.DBSCAN(enemies, 5, 2)
                    .clusters
                    .MinBy(cluster => _terrainTracker.GetClosestWalkable(cluster.GetCenter(), searchRadius: 3).DistanceTo(Context._armyCenter));

                // TODO GD Tweak this to create a concave instead?
                if (closestEnemyCluster != null && Context._armyCenter.DistanceTo(_terrainTracker.GetClosestWalkable(closestEnemyCluster.GetCenter(), searchRadius: 3)) <= OperationRadius) {
                    Context._targetPosition = _terrainTracker.GetClosestWalkable(closestEnemyCluster.GetCenter(), searchRadius: 3);
                    Context._isTargetPriority = false;
                }
            }
        }

        private bool IsReadyToEngage() {
            return HasEnoughArmyInRange() && IsArmyHealthyEnough();
        }

        private bool HasEnoughArmyInRange() {
            if (Context._targetPosition.DistanceTo(Context._armyCenter) <= EngageDistance) {
                return true;
            }

            int nbSoldiersInRange;
            if (Context._isTargetPriority) {
                nbSoldiersInRange = Context._army.Count(soldier => soldier.IsInAttackRangeOf(Context._targetPosition));
            }
            else {
                var enemyMilitaryUnits = Controller.GetUnits(_unitsTracker.EnemyUnits, Units.Military)
                    .OrderBy(enemy => enemy.DistanceTo(Context._armyCenter))
                    .ToList();

                nbSoldiersInRange = Context._army.Count(soldier => enemyMilitaryUnits.Any(soldier.IsInAttackRangeOf));
            }

            return nbSoldiersInRange >= Context._army.Count * MinimumArmyThresholdToEngage;
        }

        private bool IsArmyHealthyEnough() {
            var armyWithEnoughHealth = Context._army.Where(soldier => soldier.Integrity > MinimumIntegrityToEngage);

            return armyWithEnoughHealth.Count() >= Context._army.Count * MinimumArmyThresholdToEngage;
        }

        private void MoveArmyIntoPosition() {
            foreach (var soldier in Context._army) {
                if (!soldier.IsBurrowed) {
                    soldier.UseAbility(Abilities.BurrowRoachDown);
                }
                else {
                    soldier.Move(Context._targetPosition);
                }
            }
        }
    }
}
