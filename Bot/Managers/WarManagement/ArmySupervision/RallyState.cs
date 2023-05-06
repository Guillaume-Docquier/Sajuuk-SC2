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
        private readonly ITerrainTracker _terrainTracker;
        private readonly IGraphicalDebugger _graphicalDebugger;
        private readonly IArmySupervisorStateFactory _armySupervisorStateFactory;

        private const float AcceptableDistanceToTarget = 3;

        private float _attackAtForce;

        public RallyState(
            ITerrainTracker terrainTracker,
            IGraphicalDebugger graphicalDebugger,
            IArmySupervisorStateFactory armySupervisorStateFactory
        ) {
            _terrainTracker = terrainTracker;
            _graphicalDebugger = graphicalDebugger;
            _armySupervisorStateFactory = armySupervisorStateFactory;
        }

        protected override void OnContextSet() {
            _attackAtForce = Context._strongestForce * 1.2f;
        }

        protected override bool TryTransitioning() {
            if (Context._mainArmy.GetForce() >= _attackAtForce || Controller.SupportedSupply + 1 >= KnowledgeBase.MaxSupplyAllowed) {
                StateMachine.TransitionTo(_armySupervisorStateFactory.CreateAttackState());
                return true;
            }

            return false;
        }

        protected override void Execute() {
            DrawArmyData();

            Grow(_terrainTracker.GetClosestWalkable(Context.Army.GetCenter(), searchRadius: 3), Context.Army);
        }

        private void DrawArmyData() {
            _graphicalDebugger.AddTextGroup(
                new[]
                {
                    $"Force: {Context._mainArmy.GetForce()}",
                    $"Strongest: {Context._strongestForce}",
                    $"Attack at: {_attackAtForce}"
                },
                worldPos: _terrainTracker.WithWorldHeight(_terrainTracker.GetClosestWalkable(Context._mainArmy.GetCenter(), searchRadius: 3).Translate(1f, 1f)).ToPoint());
        }

        private void Grow(Vector2 growPosition, IReadOnlyCollection<Unit> soldiers) {
            if (soldiers.Count <= 0) {
                return;
            }

            growPosition = _terrainTracker.GetClosestWalkable(growPosition);

            _graphicalDebugger.AddSphere(_terrainTracker.WithWorldHeight(growPosition), AcceptableDistanceToTarget, Colors.Yellow);
            _graphicalDebugger.AddText("Grow", worldPos: _terrainTracker.WithWorldHeight(growPosition).ToPoint());

            soldiers.Where(unit => unit.DistanceTo(growPosition) > AcceptableDistanceToTarget)
                .ToList()
                .ForEach(unit => unit.AttackMove(growPosition));

            foreach (var soldier in soldiers) {
                _graphicalDebugger.AddLine(soldier.Position, _terrainTracker.WithWorldHeight(growPosition), Colors.Yellow);
            }
        }
    }
}
