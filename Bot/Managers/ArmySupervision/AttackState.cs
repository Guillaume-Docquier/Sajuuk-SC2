using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.Managers.ArmySupervision.Tactics;
using Bot.MapKnowledge;
using Bot.StateManagement;
using Bot.Wrapper;
using SneakAttackTactic = Bot.Managers.ArmySupervision.Tactics.SneakAttack.SneakAttackTactic;

namespace Bot.Managers.ArmySupervision;

public partial class ArmySupervisor {
    public class AttackState: State<ArmySupervisor> {
        private const float RocksDestructionRange = 9f;
        private const float AcceptableDistanceToTarget = 3;
        private const float MaxDistanceForPathfinding = 25;
        private const int PathfindingStep = 3;

        private float _initialForce;
        private float _retreatAtForce;

        private readonly StuckDetector _stuckDetector = new StuckDetector();

        private static readonly ulong MaximumPathfindingLockDelay = Controller.SecsToFrames(15);
        private bool PathfindingIsUnlocked => _pathfindingLock < Controller.Frame;
        private ulong _pathfindingLock = 0;
        private ulong _pathfindingLockDelay = Controller.SecsToFrames(4);

        private readonly ITactic _sneakAttackTactic = new SneakAttackTactic();

        protected override void OnSetStateMachine() {
            _initialForce = StateMachine.Context.Army.GetForce();
            _retreatAtForce = _initialForce * 0.5f;
        }

        protected override void OnTransition() {
            _sneakAttackTactic.Reset(null);
        }

        protected override bool TryTransitioning() {
            if (_sneakAttackTactic.IsExecuting()) {
                return false;
            }

            if (StateMachine.Context._mainArmy.GetCenter().HorizontalDistanceTo(StateMachine.Context._target) < AcceptableDistanceToTarget) {
                StateMachine.TransitionTo(new DefenseState());
                return true;
            }

            if (StateMachine.Context._mainArmy.GetForce() <= _retreatAtForce) {
                StateMachine.TransitionTo(new RetreatState());
                return true;
            }

            return false;
        }

        protected override void Execute() {
            StateMachine.Context._strongestForce = Math.Max(StateMachine.Context._strongestForce, StateMachine.Context._mainArmy.GetForce());

            DrawArmyData(StateMachine.Context._mainArmy);

            if (_sneakAttackTactic.IsViable(StateMachine.Context._mainArmy)) {
                _sneakAttackTactic.Execute(StateMachine.Context._mainArmy);
            }
            else {
                _sneakAttackTactic.Reset(StateMachine.Context._mainArmy);
                Attack(StateMachine.Context._target, StateMachine.Context._mainArmy);
            }

            Rally(StateMachine.Context._mainArmy.GetCenter(), GetSoldiersNotInMainArmy().ToList());
        }

        private void DrawArmyData(IReadOnlyCollection<Unit> soldiers) {
            if (soldiers.Count <= 0) {
                return;
            }

            Program.GraphicalDebugger.AddTextGroup(
                new[]
                {
                    $"Force: {soldiers.GetForce()}",
                    $"Initial: {_initialForce}",
                    $"Retreat at: {_retreatAtForce}"
                },
                worldPos: soldiers.GetCenter().Translate(1f, 1f).ToPoint());
        }

        private void Attack(Vector3 targetToAttack, IReadOnlyCollection<Unit> army) {
            if (army.Count <= 0) {
                return;
            }

            DrawAttackData(targetToAttack, army);

            var unitsToAttackWith = army.Where(unit => unit.IsIdleOrMovingOrAttacking())
                .Where(unit => !unit.IsBurrowed)
                .Where(unit => unit.HorizontalDistanceTo(targetToAttack) > AcceptableDistanceToTarget)
                .ToList();

            var armyLocation = army.GetCenter();
            var absoluteDistanceToTarget = armyLocation.HorizontalDistanceTo(targetToAttack);

            if (!army.IsFighting()) {
                _stuckDetector.Tick(armyLocation);
            }
            else {
                _stuckDetector.Reset(armyLocation);
            }

            // Try to take down rocks
            if (_stuckDetector.IsStuck) {
                Logger.Warning("{0} army is stuck", Name);

                var closestRock = Controller.GetUnits(UnitsTracker.NeutralUnits, Units.Destructibles).MinBy(rock => rock.HorizontalDistanceTo(armyLocation));
                if (closestRock != null) {
                    Logger.Info("{0} closest rock is {1:F2} units away", Name, closestRock.HorizontalDistanceTo(armyLocation));
                    if (closestRock.HorizontalDistanceTo(armyLocation) <= RocksDestructionRange) {
                        Logger.Info("{0} attacking nearby rock", Name);
                        Attack(closestRock, unitsToAttackWith);
                        return;
                    }
                }
                else {
                    Logger.Warning("{0} no rocks found", Name);
                }
            }

            if (absoluteDistanceToTarget <= MaxDistanceForPathfinding && !_stuckDetector.IsStuck && PathfindingIsUnlocked) {
                WalkAlongThePath(targetToAttack, armyLocation, unitsToAttackWith);
            }
            else {
                if (_stuckDetector.IsStuck) {
                    Logger.Warning("{0} disabling pathfinding for {1:F2} seconds", Name, _pathfindingLockDelay / Controller.FramesPerSecond);
                    _pathfindingLock = Controller.Frame + _pathfindingLockDelay;
                    _pathfindingLockDelay = Math.Min(MaximumPathfindingLockDelay, (ulong)(_pathfindingLockDelay * 1.25));

                    _stuckDetector.Reset(armyLocation);
                }

                AttackMove(targetToAttack, unitsToAttackWith);
            }
        }

        private static void DrawAttackData(Vector3 targetToAttack, IEnumerable<Unit> soldiers) {
            Program.GraphicalDebugger.AddSphere(targetToAttack, AcceptableDistanceToTarget, Colors.Red);
            Program.GraphicalDebugger.AddText("Attack", worldPos: targetToAttack.ToPoint());
            foreach (var soldier in soldiers) {
                Program.GraphicalDebugger.AddLine(soldier.Position, targetToAttack, Colors.Red);
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
            targetToAttack = targetToAttack.ClosestWalkable();

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

            rallyPoint = rallyPoint.ClosestWalkable();

            DrawRallyData(rallyPoint, soldiers);

            AttackMove(rallyPoint, soldiers.Where(unit => unit.HorizontalDistanceTo(rallyPoint) > AcceptableDistanceToTarget));
        }

        private static void DrawRallyData(Vector3 rallyPoint, IEnumerable<Unit> soldiers) {
            Program.GraphicalDebugger.AddSphere(rallyPoint, AcceptableDistanceToTarget, Colors.Blue);
            Program.GraphicalDebugger.AddText("Rally", worldPos: rallyPoint.ToPoint());
            foreach (var soldier in soldiers) {
                Program.GraphicalDebugger.AddLine(soldier.Position, rallyPoint, Colors.Blue);
            }
        }

        private IEnumerable<Unit> GetSoldiersNotInMainArmy() {
            return StateMachine.Context.Army.Where(soldier => !StateMachine.Context._mainArmy.Contains(soldier));
        }
    }
}
