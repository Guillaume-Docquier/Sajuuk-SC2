using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl;
using Bot.StateManagement;

namespace Bot.Managers.WarManagement.ArmySupervision;

public partial class ArmySupervisor {
    public class DefenseState: State<ArmySupervisor> {
        private const bool Debug = true;

        private readonly IVisibilityTracker _visibilityTracker;

        private const float AcceptableDistanceToTarget = 3;
        private readonly IUnitsControl _unitsController = new OffensiveUnitsControl();

        public DefenseState(IVisibilityTracker visibilityTracker) {
            _visibilityTracker = visibilityTracker;
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

            var remainingUnits = UnitsTracker.EnemyUnits
                .Where(unit => !unit.IsCloaked)
                .Where(unit => Context.CanHitAirUnits || !unit.IsFlying);

            if (remainingUnits.Any()) {
                return false;
            }

            StateMachine.TransitionTo(new HuntState(_visibilityTracker));
            return true;
        }

        protected override void Execute() {
            DrawArmyData(Context._mainArmy);

            var remainingArmy = _unitsController.Execute(new HashSet<Unit>(Context._mainArmy));

            Defend(Context._target, remainingArmy, Context._blastRadius, Context.CanHitAirUnits);
            Rally(Context._mainArmy.GetCenter(), GetSoldiersNotInMainArmy().ToList());
        }

        private static void DrawArmyData(IReadOnlyCollection<Unit> soldiers) {
            if (!Debug || soldiers.Count <= 0) {
                return;
            }

            Program.GraphicalDebugger.AddTextGroup(
                new[]
                {
                    $"Force: {soldiers.GetForce()}",
                },
                worldPos: soldiers.GetCenter().Translate(1f, 1f).ToVector3().ToPoint());
        }

        private static void Defend(Vector2 targetToDefend, IReadOnlyCollection<Unit> soldiers, float defenseRadius, bool canHitAirUnits) {
            if (soldiers.Count <= 0) {
                return;
            }

            targetToDefend = targetToDefend.ClosestWalkable();

            Program.GraphicalDebugger.AddSphere(targetToDefend.ToVector3(), AcceptableDistanceToTarget, Colors.Green);
            Program.GraphicalDebugger.AddTextGroup(new[] { "Defend", $"Radius: {defenseRadius}" }, worldPos: targetToDefend.ToVector3().ToPoint());

            var targetList = UnitsTracker.EnemyUnits
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
                        Program.GraphicalDebugger.AddLine(soldier.Position, targetToDefend.ToVector3(), Colors.Green);
                    });
            }
            else {
                Rally(targetToDefend, soldiers);
            }
        }

        private static void Rally(Vector2 rallyPoint, IReadOnlyCollection<Unit> soldiers) {
            if (soldiers.Count <= 0) {
                return;
            }

            rallyPoint = rallyPoint.ClosestWalkable();

            Program.GraphicalDebugger.AddSphere(rallyPoint.ToVector3(), AcceptableDistanceToTarget, Colors.Blue);
            Program.GraphicalDebugger.AddText("Rally", worldPos: rallyPoint.ToVector3().ToPoint());

            soldiers.Where(unit => unit.DistanceTo(rallyPoint) > AcceptableDistanceToTarget)
                .ToList()
                .ForEach(unit => unit.AttackMove(rallyPoint));

            foreach (var soldier in soldiers) {
                Program.GraphicalDebugger.AddLine(soldier.Position, rallyPoint.ToVector3(), Colors.Blue);
            }
        }

        private IEnumerable<Unit> GetSoldiersNotInMainArmy() {
            return Context.Army.Where(soldier => !Context._mainArmy.Contains(soldier));
        }
    }
}
