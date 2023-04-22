using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.StateManagement;

namespace Bot.Managers.WarManagement.ArmySupervision;

public partial class ArmySupervisor {
    public class RallyState: State<ArmySupervisor> {
        private const float AcceptableDistanceToTarget = 3;

        private readonly IVisibilityTracker _visibilityTracker;

        private float _attackAtForce;

        public RallyState(IVisibilityTracker visibilityTracker) {
            _visibilityTracker = visibilityTracker;
        }

        protected override void OnContextSet() {
            _attackAtForce = Context._strongestForce * 1.2f;
        }

        protected override bool TryTransitioning() {
            if (Context._mainArmy.GetForce() >= _attackAtForce || Controller.MaxSupply + 1 >= KnowledgeBase.MaxSupplyAllowed) {
                StateMachine.TransitionTo(new AttackState(_visibilityTracker));
                return true;
            }

            return false;
        }

        protected override void Execute() {
            DrawArmyData();

            Grow(Context.Army.GetCenter(), Context.Army);
        }

        private void DrawArmyData() {
            Program.GraphicalDebugger.AddTextGroup(
                new[]
                {
                    $"Force: {Context._mainArmy.GetForce()}",
                    $"Strongest: {Context._strongestForce}",
                    $"Attack at: {_attackAtForce}"
                },
                worldPos: Context._mainArmy.GetCenter().Translate(1f, 1f).ToVector3().ToPoint());
        }

        private static void Grow(Vector2 growPosition, IReadOnlyCollection<Unit> soldiers) {
            if (soldiers.Count <= 0) {
                return;
            }

            growPosition = growPosition.ClosestWalkable();

            Program.GraphicalDebugger.AddSphere(growPosition.ToVector3(), AcceptableDistanceToTarget, Colors.Yellow);
            Program.GraphicalDebugger.AddText("Grow", worldPos: growPosition.ToVector3().ToPoint());

            soldiers.Where(unit => unit.DistanceTo(growPosition) > AcceptableDistanceToTarget)
                .ToList()
                .ForEach(unit => unit.AttackMove(growPosition));

            foreach (var soldier in soldiers) {
                Program.GraphicalDebugger.AddLine(soldier.Position, growPosition.ToVector3(), Colors.Yellow);
            }
        }
    }
}
