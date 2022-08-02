using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.GameData;
using Bot.Managers.ArmyManagement.Tactics;
using Bot.Wrapper;

namespace Bot.Managers.ArmyManagement;

public partial class ArmyManager {
    public class AttackStrategy : IStrategy {
        private static readonly ulong ReasonableMoveDelay = Controller.SecsToFrames(5);
        private const float NegligibleMovement = 2f;
        private const float RocksDestructionRange = 9f;
        private const float AcceptableDistanceToTarget = 3;
        private const float MaxDistanceForPathfinding = 25;
        private const int PathfindingStep = 3;

        private readonly ArmyManager _armyManager;
        private readonly float _initialForce;
        private readonly float _retreatAtForce;

        private IStrategy _nextStrategy;

        private Vector3 _previousArmyLocation;
        private ulong _ticksWithoutRealMove;

        private static readonly ulong MaximumPathfindingLockDelay = Controller.SecsToFrames(15);
        private bool PathfindingIsUnlocked => _pathfindingLock < Controller.Frame;
        private ulong _pathfindingLock = 0;
        private ulong _pathfindingLockDelay = Controller.SecsToFrames(4);

        private readonly ITactic _burrowSurpriseTactic = new BurrowSurpriseTactic();

        public AttackStrategy(ArmyManager armyManager) {
            _armyManager = armyManager;
            _initialForce = _armyManager.Army.GetForce();
            _retreatAtForce = _initialForce * 0.5f;
        }

        public string Name => "Attack";

        public bool CanTransition() {
            if (_burrowSurpriseTactic.IsExecuting()) {
                return false;
            }

            if (_armyManager._mainArmy.GetCenter().DistanceTo(_armyManager._target) < AcceptableDistanceToTarget) {
                _nextStrategy = new DefenseStrategy(_armyManager);

                return true;
            }

            if (_armyManager._mainArmy.GetForce() <= _retreatAtForce) {
                _nextStrategy = new RetreatStrategy(_armyManager);

                return true;
            }

            return false;
        }

        public IStrategy Transition() {
            _burrowSurpriseTactic.Reset(null);

            return _nextStrategy;
        }

        public void Execute() {
            _armyManager._strongestForce = Math.Max(_armyManager._strongestForce, _armyManager._mainArmy.GetForce());

            DrawArmyData(_armyManager._mainArmy);

            if (_burrowSurpriseTactic.IsViable(_armyManager._mainArmy)) {
                _burrowSurpriseTactic.Execute(_armyManager._mainArmy);
            }
            else {
                _burrowSurpriseTactic.Reset(_armyManager._mainArmy);
                Attack(_armyManager._target, _armyManager._mainArmy);
            }

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

        private void Attack(Vector3 targetToAttack, IReadOnlyCollection<Unit> soldiers) {
            if (soldiers.Count <= 0) {
                return;
            }

            DrawAttackData(targetToAttack, soldiers);

            var unitsToAttackWith = soldiers.Where(unit => unit.IsIdleOrMovingOrAttacking())
                .Where(unit => !unit.RawUnitData.IsBurrowed)
                .Where(unit => unit.DistanceTo(targetToAttack) > AcceptableDistanceToTarget)
                .ToList();

            var armyLocation = soldiers.GetCenter();
            var absoluteDistanceToTarget = armyLocation.DistanceTo(targetToAttack);

            // Try to take down rocks
            var isStuck = _ticksWithoutRealMove > ReasonableMoveDelay;
            if (isStuck) {
                Logger.Warning("AttackStrategy: I'm stuck!");
                var closestRock = Controller.GetUnits(Controller.NeutralUnits, Units.Destructibles).MinBy(rock => rock.DistanceTo(armyLocation));
                if (closestRock != null) {
                    Logger.Info("AttackStrategy: Closest rock is {0} units away", closestRock.DistanceTo(armyLocation).ToString("0.00"));
                    if (closestRock.DistanceTo(armyLocation) <= RocksDestructionRange) {
                        Logger.Info("AttackStrategy: Attacking rock");
                        Attack(closestRock, unitsToAttackWith);
                        return;
                    }
                }
                else {
                    Logger.Warning("AttackStrategy: No rocks found");
                }
            }

            if (absoluteDistanceToTarget <= MaxDistanceForPathfinding && !isStuck && PathfindingIsUnlocked) {
                WalkAlongThePath(targetToAttack, armyLocation, unitsToAttackWith);
            }
            else {
                if (isStuck) {
                    Logger.Warning("AttackStrategy: disabling pathfinding for {0} seconds", (_pathfindingLockDelay / Controller.FramesPerSecond).ToString("0.00"));
                    _pathfindingLock = Controller.Frame + _pathfindingLockDelay;
                    _pathfindingLockDelay = Math.Min(MaximumPathfindingLockDelay, (ulong)(_pathfindingLockDelay * 1.25));

                    _ticksWithoutRealMove = 0;
                    _previousArmyLocation = armyLocation;
                }

                AttackMove(targetToAttack, unitsToAttackWith);
            }

            // Sometime the army gets stuck, try something different if it happens
            // This will often time be due to rocks, so I need to fix that
            if (armyLocation.DistanceTo(_previousArmyLocation) < NegligibleMovement && !soldiers.IsFighting()) {
                _ticksWithoutRealMove++;
            }
            else {
                _ticksWithoutRealMove = 0;
                _previousArmyLocation = armyLocation;
            }
        }

        private static void DrawAttackData(Vector3 targetToAttack, IEnumerable<Unit> soldiers) {
            GraphicalDebugger.AddSphere(targetToAttack, AcceptableDistanceToTarget, Colors.Red);
            GraphicalDebugger.AddText("Attack", worldPos: targetToAttack.ToPoint());
            foreach (var soldier in soldiers) {
                GraphicalDebugger.AddLine(soldier.Position, targetToAttack, Colors.Red);
            }
        }

        private static void WalkAlongThePath(Vector3 targetToAttack, Vector3 armyLocation, IEnumerable<Unit> soldiers) {
            var path = Pathfinder.FindPath(armyLocation, targetToAttack);
            if (path != null && path.Count > 0) {
                targetToAttack = path[Math.Min(path.Count - 1, PathfindingStep)];
            }

            AttackMove(targetToAttack, soldiers);
        }

        private static void AttackMove(Vector3 targetToAttack, IEnumerable<Unit> soldiers) {
            soldiers
                .ToList()
                .ForEach(unit => unit.AttackMove(targetToAttack));
        }

        private static void Attack(Unit targetToAttack, IEnumerable<Unit> soldiers) {
            soldiers
                .ToList()
                .ForEach(unit => unit.Attack(targetToAttack));
        }

        private static void Rally(Vector3 rallyPoint, IReadOnlyCollection<Unit> soldiers) {
            if (soldiers.Count <= 0) {
                return;
            }

            DrawRallyData(rallyPoint, soldiers);

            AttackMove(rallyPoint, soldiers.Where(unit => unit.DistanceTo(rallyPoint) > AcceptableDistanceToTarget));
        }

        private static void DrawRallyData(Vector3 rallyPoint, IEnumerable<Unit> soldiers) {
            GraphicalDebugger.AddSphere(rallyPoint, AcceptableDistanceToTarget, Colors.Blue);
            GraphicalDebugger.AddText("Rally", worldPos: rallyPoint.ToPoint());
            foreach (var soldier in soldiers) {
                GraphicalDebugger.AddLine(soldier.Position, rallyPoint, Colors.Blue);
            }
        }

        private IEnumerable<Unit> GetSoldiersNotInMainArmy() {
            return _armyManager.Army.Where(soldier => !_armyManager._mainArmy.Contains(soldier));
        }
    }
}
