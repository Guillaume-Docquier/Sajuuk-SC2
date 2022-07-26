using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Wrapper;

namespace Bot.Managers;

public partial class ArmyManager {
    public class AttackStrategy : IStrategy {
        private const float AcceptableDistanceToTarget = 3;

        private readonly ArmyManager _armyManager;
        private readonly float _initialForce;
        private readonly float _retreatAtForce;

        private IStrategy nextStrategy;

        public AttackStrategy(ArmyManager armyManager) {
            _armyManager = armyManager;
            _initialForce = _armyManager.Army.GetForce();
            _retreatAtForce = _initialForce * 0.5f;
        }

        public string Name => "Attack";

        public bool CanTransition() {
            if (_armyManager._mainArmy.GetCenter().DistanceTo(_armyManager._target) < AcceptableDistanceToTarget) {
                nextStrategy = new DefenseStrategy(_armyManager);

                return true;
            }

            if (_armyManager._mainArmy.GetForce() <= _retreatAtForce) {
                nextStrategy = new RetreatStrategy(_armyManager);

                return true;
            }

            return false;
        }

        public IStrategy Transition() {
            return nextStrategy;
        }

        public void Execute() {
            _armyManager._strongestForce = Math.Max(_armyManager._strongestForce, _armyManager._mainArmy.GetForce());

            DrawArmyData(_armyManager._mainArmy);

            Attack(_armyManager._target, _armyManager._mainArmy);
            Rally(_armyManager._mainArmy.GetCenter(), GetSoldiersNotInMainArmy().ToList());
        }

        private void DrawArmyData(IReadOnlyCollection<Unit> soldiers) {
            if (soldiers.Count <= 0) {
                return;
            }

            GraphicalDebugger.AddTextGroup(
                new[]
                {
                    $"Force: {soldiers.GetForce()}",
                    $"Initial: {_initialForce}",
                    $"Retreat at: {_retreatAtForce}"
                },
                worldPos: soldiers.GetCenter().Translate(1f, 1f).ToPoint());
        }

        private static void Attack(Vector3 targetToAttack, IReadOnlyCollection<Unit> soldiers) {
            if (soldiers.Count <= 0) {
                return;
            }

            GraphicalDebugger.AddSphere(targetToAttack, AcceptableDistanceToTarget, Colors.Red);
            GraphicalDebugger.AddText("Attack", worldPos: targetToAttack.ToPoint());

            soldiers.Where(unit => unit.IsMovingOrAttacking())
                .Where(unit => !unit.IsAlreadyTargeting(targetToAttack))
                .Where(unit => !unit.RawUnitData.IsBurrowed)
                .Where(unit => unit.DistanceTo(targetToAttack) > AcceptableDistanceToTarget)
                .ToList()
                .ForEach(unit => unit.AttackMove(targetToAttack));

            foreach (var soldier in soldiers) {
                GraphicalDebugger.AddLine(soldier.Position, targetToAttack, Colors.Red);
            }
        }

        private static void Rally(Vector3 rallyPoint, IReadOnlyCollection<Unit> soldiers) {
            if (soldiers.Count <= 0) {
                return;
            }

            GraphicalDebugger.AddSphere(rallyPoint, AcceptableDistanceToTarget, Colors.Blue);
            GraphicalDebugger.AddText("Rally", worldPos: rallyPoint.ToPoint());

            soldiers.Where(unit => !unit.IsAlreadyTargeting(rallyPoint))
                .Where(unit => unit.DistanceTo(rallyPoint) > AcceptableDistanceToTarget)
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
