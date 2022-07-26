﻿using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.GameData;
using Bot.Wrapper;

namespace Bot.Managers;

public partial class ArmyManager {
    public class RetreatStrategy: IStrategy {
        private const float AcceptableDistanceToTarget = 3;

        private readonly ArmyManager _armyManager;
        private readonly float _rallyAtForce;

        public RetreatStrategy(ArmyManager armyManager) {
            _armyManager = armyManager;
            _rallyAtForce = _armyManager._strongestForce;
        }

        public string Name => "Retreat";

        public bool CanTransition() {
            return _armyManager._mainArmy.GetForce() >= _rallyAtForce
                   || Controller.MaxSupply + 1 >= KnowledgeBase.MaxSupplyAllowed
                   || _armyManager._mainArmy.GetCenter().DistanceTo(GetRetreatPosition()) <= AcceptableDistanceToTarget;
        }

        public IStrategy Transition() {
            return new RallyStrategy(_armyManager);
        }

        public void Execute() {
            DrawArmyData();
            // TODO GD Retreat to base, cache the pathfinding
            Retreat(GetRetreatPosition(), _armyManager.Army);
        }

        private void DrawArmyData() {
            GraphicalDebugger.AddTextGroup(
                new[]
                {
                    $"Force: {_armyManager._mainArmy.GetForce()}",
                    $"Strongest: {_armyManager._strongestForce}",
                    $"Rally at: {_rallyAtForce}"
                },
                worldPos: _armyManager._mainArmy.GetCenter().Translate(1f, 1f).ToPoint());
        }

        private static void Retreat(Vector3 retreatPosition, IReadOnlyCollection<Unit> soldiers) {
            if (soldiers.Count <= 0) {
                return;
            }

            GraphicalDebugger.AddSphere(retreatPosition, AcceptableDistanceToTarget, Colors.Yellow);
            GraphicalDebugger.AddText("Retreat", worldPos: retreatPosition.ToPoint());

            soldiers.Where(unit => !unit.IsAlreadyTargeting(retreatPosition))
                .Where(unit => unit.DistanceTo(retreatPosition) > AcceptableDistanceToTarget)
                .ToList()
                .ForEach(unit => unit.Move(retreatPosition));

            foreach (var soldier in soldiers) {
                GraphicalDebugger.AddLine(soldier.Position, retreatPosition, Colors.Yellow);
            }
        }

        private Vector3 GetRetreatPosition() {
            return _armyManager.Army.GetCenter();
        }
    }
}
