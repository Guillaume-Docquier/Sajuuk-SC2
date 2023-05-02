using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.GameSense.RegionsEvaluationsTracking;
using Bot.StateManagement;

namespace Bot.Managers.WarManagement.ArmySupervision;

public partial class ArmySupervisor {
    public class RallyState: State<ArmySupervisor> {
        private readonly IVisibilityTracker _visibilityTracker;
        private readonly IUnitsTracker _unitsTracker;
        private readonly ITerrainTracker _terrainTracker;
        private readonly IRegionsTracker _regionsTracker;
        private readonly IRegionsEvaluationsTracker _regionsEvaluationsTracker;

        private const float AcceptableDistanceToTarget = 3;

        private float _attackAtForce;

        public RallyState(
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
        }

        protected override void OnContextSet() {
            _attackAtForce = Context._strongestForce * 1.2f;
        }

        protected override bool TryTransitioning() {
            if (Context._mainArmy.GetForce() >= _attackAtForce || Controller.SupportedSupply + 1 >= KnowledgeBase.MaxSupplyAllowed) {
                StateMachine.TransitionTo(new AttackState(_visibilityTracker, _unitsTracker, _terrainTracker, _regionsTracker, _regionsEvaluationsTracker));
                return true;
            }

            return false;
        }

        protected override void Execute() {
            DrawArmyData();

            Grow(_terrainTracker.GetClosestWalkable(Context.Army.GetCenter(), searchRadius: 3), Context.Army);
        }

        private void DrawArmyData() {
            Program.GraphicalDebugger.AddTextGroup(
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

            Program.GraphicalDebugger.AddSphere(_terrainTracker.WithWorldHeight(growPosition), AcceptableDistanceToTarget, Colors.Yellow);
            Program.GraphicalDebugger.AddText("Grow", worldPos: _terrainTracker.WithWorldHeight(growPosition).ToPoint());

            soldiers.Where(unit => unit.DistanceTo(growPosition) > AcceptableDistanceToTarget)
                .ToList()
                .ForEach(unit => unit.AttackMove(growPosition));

            foreach (var soldier in soldiers) {
                Program.GraphicalDebugger.AddLine(soldier.Position, _terrainTracker.WithWorldHeight(growPosition), Colors.Yellow);
            }
        }
    }
}
