using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.GameSense.RegionTracking;
using Bot.MapKnowledge;
using Bot.StateManagement;

namespace Bot.Managers.WarManagement.ArmySupervision;

public partial class ArmySupervisor {
    public class RallyState: State<ArmySupervisor> {
        private readonly IVisibilityTracker _visibilityTracker;
        private readonly IUnitsTracker _unitsTracker;
        private readonly IMapAnalyzer _mapAnalyzer;
        private readonly IExpandAnalyzer _expandAnalyzer;
        private readonly IRegionAnalyzer _regionAnalyzer;
        private readonly IRegionTracker _regionTracker;

        private const float AcceptableDistanceToTarget = 3;

        private float _attackAtForce;

        public RallyState(
            IVisibilityTracker visibilityTracker,
            IUnitsTracker unitsTracker,
            IMapAnalyzer mapAnalyzer,
            IExpandAnalyzer expandAnalyzer,
            IRegionAnalyzer regionAnalyzer,
            IRegionTracker regionTracker
        ) {
            _visibilityTracker = visibilityTracker;
            _unitsTracker = unitsTracker;
            _mapAnalyzer = mapAnalyzer;
            _expandAnalyzer = expandAnalyzer;
            _regionAnalyzer = regionAnalyzer;
            _regionTracker = regionTracker;
        }

        protected override void OnContextSet() {
            _attackAtForce = Context._strongestForce * 1.2f;
        }

        protected override bool TryTransitioning() {
            if (Context._mainArmy.GetForce() >= _attackAtForce || Controller.SupportedSupply + 1 >= KnowledgeBase.MaxSupplyAllowed) {
                StateMachine.TransitionTo(new AttackState(_visibilityTracker, _unitsTracker, _mapAnalyzer, _expandAnalyzer, _regionAnalyzer, _regionTracker));
                return true;
            }

            return false;
        }

        protected override void Execute() {
            DrawArmyData();

            Grow(_mapAnalyzer.GetClosestWalkable(Context.Army.GetCenter(), searchRadius: 3), Context.Army);
        }

        private void DrawArmyData() {
            Program.GraphicalDebugger.AddTextGroup(
                new[]
                {
                    $"Force: {Context._mainArmy.GetForce()}",
                    $"Strongest: {Context._strongestForce}",
                    $"Attack at: {_attackAtForce}"
                },
                worldPos: _mapAnalyzer.WithWorldHeight(_mapAnalyzer.GetClosestWalkable(Context._mainArmy.GetCenter(), searchRadius: 3).Translate(1f, 1f)).ToPoint());
        }

        private void Grow(Vector2 growPosition, IReadOnlyCollection<Unit> soldiers) {
            if (soldiers.Count <= 0) {
                return;
            }

            growPosition = _mapAnalyzer.GetClosestWalkable(growPosition);

            Program.GraphicalDebugger.AddSphere(_mapAnalyzer.WithWorldHeight(growPosition), AcceptableDistanceToTarget, Colors.Yellow);
            Program.GraphicalDebugger.AddText("Grow", worldPos: _mapAnalyzer.WithWorldHeight(growPosition).ToPoint());

            soldiers.Where(unit => unit.DistanceTo(growPosition) > AcceptableDistanceToTarget)
                .ToList()
                .ForEach(unit => unit.AttackMove(growPosition));

            foreach (var soldier in soldiers) {
                Program.GraphicalDebugger.AddLine(soldier.Position, _mapAnalyzer.WithWorldHeight(growPosition), Colors.Yellow);
            }
        }
    }
}
