using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.GameData;
using Bot.Wrapper;

namespace Bot.Managers;

public partial class ArmyManager {
    public class RallyStrategy: IStrategy {
        private const float AcceptableDistanceToTarget = 3;

        private readonly ArmyManager _armyManager;
        private readonly float _attackAtForce;

        public RallyStrategy(ArmyManager armyManager) {
            _armyManager = armyManager;
            _attackAtForce = armyManager._strongestForce * 1.2f;
        }

        public string Name => "Rally";

        public bool CanTransition() {
            return _armyManager._mainArmy.GetForce() >= _attackAtForce || Controller.MaxSupply + 1 >= KnowledgeBase.MaxSupplyAllowed;
        }

        public IStrategy Transition() {
            return new AttackStrategy(_armyManager);
        }

        public void Execute() {
            DrawArmyData();

            Grow(_armyManager.Army.GetCenter(), _armyManager.Army);
        }

        private void DrawArmyData() {
            GraphicalDebugger.AddTextGroup(
                new[]
                {
                    $"Force: {_armyManager._mainArmy.GetForce()}",
                    $"Strongest: {_armyManager._strongestForce}",
                    $"Attack at: {_attackAtForce}"
                },
                worldPos: _armyManager._mainArmy.GetCenter().Translate(1f, 1f).ToPoint());
        }

        private static void Grow(Vector3 growPosition, IReadOnlyCollection<Unit> soldiers) {
            if (soldiers.Count <= 0) {
                return;
            }

            GraphicalDebugger.AddSphere(growPosition, AcceptableDistanceToTarget, Colors.Yellow);
            GraphicalDebugger.AddText("Grow", worldPos: growPosition.ToPoint());

            soldiers.Where(unit => unit.DistanceTo(growPosition) > AcceptableDistanceToTarget)
                .ToList()
                .ForEach(unit => unit.AttackMove(growPosition));

            foreach (var soldier in soldiers) {
                GraphicalDebugger.AddLine(soldier.Position, growPosition, Colors.Yellow);
            }
        }
    }
}
