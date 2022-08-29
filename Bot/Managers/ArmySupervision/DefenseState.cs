using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.StateManagement;
using Bot.Wrapper;

namespace Bot.Managers.ArmySupervision;

public partial class ArmySupervisor {
    public class DefenseState: State<ArmySupervisor> {
        private const float AcceptableDistanceToTarget = 3;

        protected override bool TryTransitioning() {
            if (StateMachine.Context._canHuntTheEnemy && UnitsTracker.EnemyUnits.All(enemy => enemy.RawUnitData.IsFlying)) { // TODO GD Handle air units
                StateMachine.TransitionTo(new HuntState());
                return true;
            }

            return false;
        }

        protected override void Execute() {
            DrawArmyData(StateMachine.Context._mainArmy);

            Defend(StateMachine.Context._target, StateMachine.Context._mainArmy, StateMachine.Context._blastRadius);
            Rally(StateMachine.Context._mainArmy.GetCenter(), GetSoldiersNotInMainArmy().ToList());
        }

        private static void DrawArmyData(IReadOnlyCollection<Unit> soldiers) {
            if (soldiers.Count <= 0) {
                return;
            }

            GraphicalDebugger.AddTextGroup(
                new[]
                {
                    $"Force: {soldiers.GetForce()}",
                },
                worldPos: soldiers.GetCenter().Translate(1f, 1f).ToPoint());
        }

        private static void Defend(Vector3 targetToDefend, IReadOnlyCollection<Unit> soldiers, float defenseRadius) {
            if (soldiers.Count <= 0) {
                return;
            }

            targetToDefend = targetToDefend.ClosestWalkable();

            GraphicalDebugger.AddSphere(targetToDefend, AcceptableDistanceToTarget, Colors.Green);
            GraphicalDebugger.AddTextGroup(new[] { "Defend", $"Radius: {defenseRadius}" }, worldPos: targetToDefend.ToPoint());

            var targetList = UnitsTracker.EnemyUnits
                .Where(enemy => !enemy.RawUnitData.IsFlying) // TODO GD Some units should hit these
                .Where(enemy => enemy.HorizontalDistanceTo(targetToDefend) < defenseRadius)
                .OrderBy(enemy => enemy.HorizontalDistanceTo(targetToDefend))
                .ToList();

            if (targetList.Any()) {
                soldiers.Where(unit => unit.IsIdleOrMovingOrAttacking())
                    .Where(unit => !unit.RawUnitData.IsBurrowed)
                    .ToList()
                    .ForEach(soldier => {
                        var closestEnemy = targetList.Take(5).OrderBy(enemy => enemy.HorizontalDistanceTo(soldier)).First();

                        soldier.AttackMove(closestEnemy.Position);
                        GraphicalDebugger.AddLine(soldier.Position, closestEnemy.Position, Colors.Red);
                        GraphicalDebugger.AddLine(soldier.Position, targetToDefend, Colors.Green);
                    });
            }
            else {
                Rally(targetToDefend, soldiers);
            }
        }

        private static void Rally(Vector3 rallyPoint, IReadOnlyCollection<Unit> soldiers) {
            if (soldiers.Count <= 0) {
                return;
            }

            rallyPoint = rallyPoint.ClosestWalkable();

            GraphicalDebugger.AddSphere(rallyPoint, AcceptableDistanceToTarget, Colors.Blue);
            GraphicalDebugger.AddText("Rally", worldPos: rallyPoint.ToPoint());

            soldiers.Where(unit => unit.HorizontalDistanceTo(rallyPoint) > AcceptableDistanceToTarget)
                .ToList()
                .ForEach(unit => unit.AttackMove(rallyPoint));

            foreach (var soldier in soldiers) {
                GraphicalDebugger.AddLine(soldier.Position, rallyPoint, Colors.Blue);
            }
        }

        private IEnumerable<Unit> GetSoldiersNotInMainArmy() {
            return StateMachine.Context.Army.Where(soldier => !StateMachine.Context._mainArmy.Contains(soldier));
        }
    }
}
