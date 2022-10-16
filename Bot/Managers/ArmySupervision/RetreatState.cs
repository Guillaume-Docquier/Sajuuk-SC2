using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapKnowledge;
using Bot.StateManagement;

namespace Bot.Managers.ArmySupervision;

public partial class ArmySupervisor {
    public class RetreatState: State<ArmySupervisor> {
        private const int RetreatDistanceFromBase = 8;
        private const float AcceptableDistanceToTarget = 3;

        private float _rallyAtForce;

        protected override void OnSetStateMachine() {
            _rallyAtForce = StateMachine.Context._strongestForce;
        }

        protected override bool TryTransitioning() {
            if (StateMachine.Context.Army.GetForce() >= _rallyAtForce
                || Controller.MaxSupply + 1 >= KnowledgeBase.MaxSupplyAllowed
                || StateMachine.Context._mainArmy.GetCenter().HorizontalDistanceTo(GetRetreatPosition()) <= AcceptableDistanceToTarget) {
                StateMachine.TransitionTo(new RallyState());
                return true;
            }

            return false;
        }

        protected override void Execute() {
            DrawArmyData();

            Retreat(GetRetreatPosition(), StateMachine.Context.Army);
        }

        private void DrawArmyData() {
            Program.GraphicalDebugger.AddTextGroup(
                new[]
                {
                    $"Force: {StateMachine.Context._mainArmy.GetForce()}",
                    $"Strongest: {StateMachine.Context._strongestForce}",
                    $"Rally at: {_rallyAtForce}"
                },
                worldPos: StateMachine.Context._mainArmy.GetCenter().Translate(1f, 1f).ToPoint());
        }

        private static void Retreat(Vector3 retreatPosition, IReadOnlyCollection<Unit> soldiers) {
            if (soldiers.Count <= 0) {
                return;
            }

            Program.GraphicalDebugger.AddSphere(retreatPosition, AcceptableDistanceToTarget, Colors.Yellow);
            Program.GraphicalDebugger.AddText("Retreat", worldPos: retreatPosition.ToPoint());

            soldiers.Where(unit => unit.HorizontalDistanceTo(retreatPosition) > AcceptableDistanceToTarget)
                .ToList()
                .ForEach(unit => unit.Move(retreatPosition));

            foreach (var soldier in soldiers) {
                Program.GraphicalDebugger.AddLine(soldier.Position, retreatPosition, Colors.Yellow);
            }
        }

        private static Vector3 GetRetreatPosition() {
            var shortestPathBetweenBaseAndEnemy = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery)
                .Where(townHall => Pathfinder.FindPath(townHall.Position, MapAnalyzer.EnemyStartingLocation) != null)
                .Select(townHall => Pathfinder.FindPath(townHall.Position, MapAnalyzer.EnemyStartingLocation))
                .MinBy(path => path.Count);

            shortestPathBetweenBaseAndEnemy ??= Pathfinder.FindPath(MapAnalyzer.StartingLocation, MapAnalyzer.EnemyStartingLocation);

            return shortestPathBetweenBaseAndEnemy[RetreatDistanceFromBase];
        }
    }
}
