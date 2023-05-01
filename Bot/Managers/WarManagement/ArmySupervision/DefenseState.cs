using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.GameSense.RegionTracking;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl;
using Bot.StateManagement;

namespace Bot.Managers.WarManagement.ArmySupervision;

public partial class ArmySupervisor {
    public class DefenseState: State<ArmySupervisor> {
        private readonly IVisibilityTracker _visibilityTracker;
        private readonly IUnitsTracker _unitsTracker;
        private readonly ITerrainTracker _terrainTracker;
        private readonly IRegionsTracker _regionsTracker;
        private readonly IRegionsEvaluationsTracker _regionsEvaluationsTracker;

        private const bool Debug = true;

        private const float AcceptableDistanceToTarget = 3;
        private readonly IUnitsControl _unitsController;

        public DefenseState(
            IVisibilityTracker visibilityTracker,
            IUnitsTracker unitsTracker,
            ITerrainTracker terrainTracker,
            IRegionsTracker regionsTracker,
            IRegionsEvaluationsTracker regionsEvaluationsTracker
        ) {
            _visibilityTracker = visibilityTracker;
            _unitsTracker = unitsTracker;
            _terrainTracker = terrainTracker;
            _regionsTracker = regionsTracker;
            _regionsEvaluationsTracker = regionsEvaluationsTracker;

            _unitsController = new OffensiveUnitsControl(_unitsTracker, _terrainTracker, _regionsTracker, _regionsEvaluationsTracker);
        }

        protected override void OnTransition() {
            _unitsController.Reset(ImmutableList<Unit>.Empty);
        }

        protected override bool TryTransitioning() {
            if (_unitsController.IsExecuting()) {
                return false;
            }

            if (!Context._canHuntTheEnemy) {
                return false;
            }

            var remainingUnits = _unitsTracker.EnemyUnits
                .Where(unit => !unit.IsCloaked)
                .Where(unit => Context.CanHitAirUnits || !unit.IsFlying);

            if (remainingUnits.Any()) {
                return false;
            }

            StateMachine.TransitionTo(new HuntState(_visibilityTracker, _unitsTracker, _terrainTracker, _regionsTracker, _regionsEvaluationsTracker));
            return true;
        }

        protected override void Execute() {
            DrawArmyData(Context._mainArmy);

            var remainingArmy = _unitsController.Execute(new HashSet<Unit>(Context._mainArmy));

            Defend(Context._target, remainingArmy, Context._blastRadius, Context.CanHitAirUnits);
            Rally(_terrainTracker.GetClosestWalkable(Context._mainArmy.GetCenter(), searchRadius: 3), GetSoldiersNotInMainArmy().ToList());
        }

        private void DrawArmyData(IReadOnlyCollection<Unit> soldiers) {
            if (!Debug || soldiers.Count <= 0) {
                return;
            }

            Program.GraphicalDebugger.AddTextGroup(
                new[]
                {
                    $"Force: {soldiers.GetForce()}",
                },
                worldPos: _terrainTracker.WithWorldHeight(_terrainTracker.GetClosestWalkable(soldiers.GetCenter(), searchRadius: 3).Translate(1f, 1f)).ToPoint());
        }

        private void Defend(Vector2 targetToDefend, IReadOnlyCollection<Unit> soldiers, float defenseRadius, bool canHitAirUnits) {
            if (soldiers.Count <= 0) {
                return;
            }

            targetToDefend = _terrainTracker.GetClosestWalkable(targetToDefend);

            Program.GraphicalDebugger.AddSphere(_terrainTracker.WithWorldHeight(targetToDefend), AcceptableDistanceToTarget, Colors.Green);
            Program.GraphicalDebugger.AddTextGroup(new[] { "Defend", $"Radius: {defenseRadius}" }, worldPos: _terrainTracker.WithWorldHeight(targetToDefend).ToPoint());

            var targetList = _unitsTracker.EnemyUnits
                .Where(unit => !unit.IsCloaked)
                .Where(unit => canHitAirUnits || !unit.IsFlying)
                .Where(enemy => enemy.DistanceTo(targetToDefend) < defenseRadius)
                .OrderBy(enemy => enemy.DistanceTo(targetToDefend))
                .ToList();

            if (targetList.Any()) {
                soldiers.Where(unit => unit.IsIdle() || unit.IsMoving() || unit.IsAttacking() || unit.IsMineralWalking())
                    .Where(unit => !unit.IsBurrowed)
                    .ToList()
                    .ForEach(soldier => {
                        var closestEnemy = targetList.Take(5).OrderBy(enemy => enemy.DistanceTo(soldier)).First();

                        soldier.AttackMove(closestEnemy.Position.ToVector2());
                        Program.GraphicalDebugger.AddLine(soldier.Position, closestEnemy.Position, Colors.Red);
                        Program.GraphicalDebugger.AddLine(soldier.Position, _terrainTracker.WithWorldHeight(targetToDefend), Colors.Green);
                    });
            }
            else {
                Rally(targetToDefend, soldiers);
            }
        }

        private void Rally(Vector2 rallyPoint, IReadOnlyCollection<Unit> soldiers) {
            if (soldiers.Count <= 0) {
                return;
            }

            rallyPoint = _terrainTracker.GetClosestWalkable(rallyPoint);

            Program.GraphicalDebugger.AddSphere(_terrainTracker.WithWorldHeight(rallyPoint), AcceptableDistanceToTarget, Colors.Blue);
            Program.GraphicalDebugger.AddText("Rally", worldPos: _terrainTracker.WithWorldHeight(rallyPoint).ToPoint());

            soldiers.Where(unit => unit.DistanceTo(rallyPoint) > AcceptableDistanceToTarget)
                .ToList()
                .ForEach(unit => unit.AttackMove(rallyPoint));

            foreach (var soldier in soldiers) {
                Program.GraphicalDebugger.AddLine(soldier.Position, _terrainTracker.WithWorldHeight(rallyPoint), Colors.Blue);
            }
        }

        private IEnumerable<Unit> GetSoldiersNotInMainArmy() {
            return Context.Army.Where(soldier => !Context._mainArmy.Contains(soldier));
        }
    }
}
