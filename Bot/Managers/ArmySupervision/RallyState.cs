using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.StateManagement;
using Bot.Wrapper;

namespace Bot.Managers.ArmySupervision;

public partial class ArmySupervisor {
    public class RallyState: State<ArmySupervisor> {
        private const float AcceptableDistanceToTarget = 3;

        private float _attackAtForce;

        protected override void OnSetStateMachine() {
            _attackAtForce = StateMachine.Context._strongestForce * 1.2f;
        }

        protected override bool TryTransitioning() {
            if (StateMachine.Context._mainArmy.GetForce() >= _attackAtForce || Controller.MaxSupply + 1 >= KnowledgeBase.MaxSupplyAllowed) {
                StateMachine.TransitionTo(new AttackState());
                return true;
            }

            return false;
        }

        protected override void Execute() {
            DrawArmyData();

            Grow(StateMachine.Context.Army.GetCenter(), StateMachine.Context.Army);
        }

        private void DrawArmyData() {
            GraphicalDebugger.AddTextGroup(
                new[]
                {
                    $"Force: {StateMachine.Context._mainArmy.GetForce()}",
                    $"Strongest: {StateMachine.Context._strongestForce}",
                    $"Attack at: {_attackAtForce}"
                },
                worldPos: StateMachine.Context._mainArmy.GetCenter().Translate(1f, 1f).ToPoint());
        }

        private static void Grow(Vector3 growPosition, IReadOnlyCollection<Unit> soldiers) {
            if (soldiers.Count <= 0) {
                return;
            }

            growPosition = growPosition.ClosestWalkable();

            GraphicalDebugger.AddSphere(growPosition, AcceptableDistanceToTarget, Colors.Yellow);
            GraphicalDebugger.AddText("Grow", worldPos: growPosition.ToPoint());

            soldiers.Where(unit => unit.HorizontalDistanceTo(growPosition) > AcceptableDistanceToTarget)
                .ToList()
                .ForEach(unit => unit.AttackMove(growPosition));

            foreach (var soldier in soldiers) {
                GraphicalDebugger.AddLine(soldier.Position, growPosition, Colors.Yellow);
            }
        }
    }
}
