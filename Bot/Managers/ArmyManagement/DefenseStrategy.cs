using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.Wrapper;

namespace Bot.Managers.ArmyManagement;

public partial class ArmyManager {
    public class DefenseStrategy: IStrategy {
        private const float AcceptableDistanceToTarget = 3;

        private readonly ArmyManager _armyManager;

        public DefenseStrategy(ArmyManager armyManager) {
            _armyManager = armyManager;
        }

        public string Name => "Defend";

        public bool CanTransition() {
            return _armyManager._canHuntTheEnemy && UnitsTracker.EnemyUnits.All(enemy => enemy.RawUnitData.IsFlying); // TODO GD Handle air units
        }

        public IStrategy Transition() {
            return new HuntStrategy(_armyManager);
        }

        public void Execute() {
            DrawArmyData(_armyManager._mainArmy);

            Defend(_armyManager._target, _armyManager._mainArmy, _armyManager._blastRadius);
            Rally(_armyManager._mainArmy.GetCenter(), GetSoldiersNotInMainArmy().ToList());
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
            return _armyManager.Army.Where(soldier => !_armyManager._mainArmy.Contains(soldier));
        }
    }
}
