using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.StateManagement;

namespace Bot.Managers.ArmySupervision;

public partial class ArmySupervisor {
    public class DefenseState: State<ArmySupervisor> {
        private const float AcceptableDistanceToTarget = 3;

        protected override bool TryTransitioning() {
            if (StateMachine.Context._canHuntTheEnemy) {
                var remainingUnits = UnitsTracker.EnemyUnits
                    .Where(unit => !unit.IsCloaked)
                    .Where(unit => StateMachine.Context.CanHitAirUnits || !unit.IsFlying);

                if (!remainingUnits.Any()) {
                    StateMachine.TransitionTo(new HuntState());
                    return true;
                }
            }

            return false;
        }

        protected override void Execute() {
            DrawArmyData(StateMachine.Context._mainArmy);

            Defend(StateMachine.Context._target, StateMachine.Context._mainArmy, StateMachine.Context._blastRadius, StateMachine.Context.CanHitAirUnits);
            Rally(StateMachine.Context._mainArmy.GetCenter(), GetSoldiersNotInMainArmy().ToList());
        }

        private static void DrawArmyData(IReadOnlyCollection<Unit> soldiers) {
            if (soldiers.Count <= 0) {
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
                soldiers.Where(unit => unit.IsIdleOrMovingOrAttacking())
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
            return StateMachine.Context.Army.Where(soldier => !StateMachine.Context._mainArmy.Contains(soldier));
        }
    }
}
