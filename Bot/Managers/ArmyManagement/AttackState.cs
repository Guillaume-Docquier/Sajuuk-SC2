using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.Managers.ArmyManagement.Tactics;
using Bot.Managers.ArmyManagement.Tactics.SneakAttack;
using Bot.MapKnowledge;
using Bot.StateManagement;
using Bot.Wrapper;

namespace Bot.Managers.ArmyManagement;

public partial class ArmyManager {
    public class AttackState: State<ArmyManager> {
        private static readonly ulong ReasonableMoveDelay = Controller.SecsToFrames(5);
        private const float NegligibleMovement = 2f;
        private const float RocksDestructionRange = 9f;
        private const float AcceptableDistanceToTarget = 3;
        private const float MaxDistanceForPathfinding = 25;
        private const int PathfindingStep = 3;

        private float _initialForce;
        private float _retreatAtForce;

        private Vector3 _previousArmyLocation;
        private ulong _ticksWithoutRealMove;

        private static readonly ulong MaximumPathfindingLockDelay = Controller.SecsToFrames(15);
        private bool PathfindingIsUnlocked => _pathfindingLock < Controller.Frame;
        private ulong _pathfindingLock = 0;
        private ulong _pathfindingLockDelay = Controller.SecsToFrames(4);

        private readonly ITactic _sneakAttackTactic = new SneakAttackTactic();

        protected override void OnSetStateMachine() {
            _initialForce = StateMachine.Army.GetForce();
            _retreatAtForce = _initialForce * 0.5f;
        }

        protected override void OnTransition() {
            _sneakAttackTactic.Reset(null);
        }

        protected override bool TryTransitioning() {
            if (_sneakAttackTactic.IsExecuting()) {
                return false;
            }

            if (StateMachine._mainArmy.GetCenter().HorizontalDistanceTo(StateMachine._target) < AcceptableDistanceToTarget) {
                StateMachine.TransitionTo(new DefenseState());
                return true;
            }

            if (StateMachine._mainArmy.GetForce() <= _retreatAtForce) {
                StateMachine.TransitionTo(new RetreatState());
                return true;
            }

            return false;
        }

        protected override void Execute() {
            StateMachine._strongestForce = Math.Max(StateMachine._strongestForce, StateMachine._mainArmy.GetForce());

            DrawArmyData(StateMachine._mainArmy);

            if (_sneakAttackTactic.IsViable(StateMachine._mainArmy)) {
                _sneakAttackTactic.Execute(StateMachine._mainArmy);
            }
            else {
                _sneakAttackTactic.Reset(StateMachine._mainArmy);
                Attack(StateMachine._target, StateMachine._mainArmy);
            }

            Rally(StateMachine._mainArmy.GetCenter(), GetSoldiersNotInMainArmy().ToList());
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
                .Where(unit => unit.HorizontalDistanceTo(targetToAttack) > AcceptableDistanceToTarget)
                .ToList();

            var armyLocation = soldiers.GetCenter();
            var absoluteDistanceToTarget = armyLocation.HorizontalDistanceTo(targetToAttack);

            // Try to take down rocks
            var isStuck = _ticksWithoutRealMove > ReasonableMoveDelay;
            if (isStuck) {
                Logger.Warning("AttackStrategy: I'm stuck!");
                var closestRock = Controller.GetUnits(UnitsTracker.NeutralUnits, Units.Destructibles).MinBy(rock => rock.HorizontalDistanceTo(armyLocation));
                if (closestRock != null) {
                    Logger.Info("AttackStrategy: Closest rock is {0} units away", closestRock.HorizontalDistanceTo(armyLocation).ToString("0.00"));
                    if (closestRock.HorizontalDistanceTo(armyLocation) <= RocksDestructionRange) {
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
            if (armyLocation.HorizontalDistanceTo(_previousArmyLocation) < NegligibleMovement && !soldiers.IsFighting()) {
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
            GraphicalDebugger.AddSphere(rallyPoint, AcceptableDistanceToTarget, Colors.Blue);
            GraphicalDebugger.AddText("Rally", worldPos: rallyPoint.ToPoint());
            foreach (var soldier in soldiers) {
                GraphicalDebugger.AddLine(soldier.Position, rallyPoint, Colors.Blue);
            }
        }

        private IEnumerable<Unit> GetSoldiersNotInMainArmy() {
            return StateMachine.Army.Where(soldier => !StateMachine._mainArmy.Contains(soldier));
        }
    }
}
