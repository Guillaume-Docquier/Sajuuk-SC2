using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl;
using Bot.MapKnowledge;
using Bot.StateManagement;
using Bot.Utils;

namespace Bot.Managers.WarManagement.ArmySupervision;

public partial class ArmySupervisor {
    public class AttackState: State<ArmySupervisor> {
        private const bool Debug = true;

        private readonly IVisibilityTracker _visibilityTracker;
        private readonly IUnitsTracker _unitsTracker;
        private readonly IMapAnalyzer _mapAnalyzer;

        private const float RocksDestructionRange = 9f;
        private const float AcceptableDistanceToTarget = 3;
        private const float MaxDistanceForPathfinding = 25;
        private const int PathfindingStep = 3;

        private readonly StuckDetector _stuckDetector = new StuckDetector();

        private static readonly ulong MaximumPathfindingLockDelay = TimeUtils.SecsToFrames(15);
        private bool PathfindingIsUnlocked => _pathfindingLock < Controller.Frame;
        private ulong _pathfindingLock = 0;
        private ulong _pathfindingLockDelay = TimeUtils.SecsToFrames(4);

        private readonly IUnitsControl _unitsController;

        public AttackState(IVisibilityTracker visibilityTracker, IUnitsTracker unitsTracker, IMapAnalyzer mapAnalyzer) {
            _visibilityTracker = visibilityTracker;
            _unitsTracker = unitsTracker;
            _mapAnalyzer = mapAnalyzer;

            _unitsController = new OffensiveUnitsControl(_unitsTracker, _mapAnalyzer);
        }

        protected override void OnTransition() {
            _unitsController.Reset(ImmutableList<Unit>.Empty);
        }

        protected override bool TryTransitioning() {
            if (_unitsController.IsExecuting()) {
                return false;
            }

            if (_mapAnalyzer.GetClosestWalkable(Context._mainArmy.GetCenter(), searchRadius: 3).DistanceTo(Context._target) < AcceptableDistanceToTarget) {
                StateMachine.TransitionTo(new DefenseState(_visibilityTracker, _unitsTracker, _mapAnalyzer));
                return true;
            }

            return false;
        }

        protected override void Execute() {
            Context._strongestForce = Math.Max(Context._strongestForce, Context._mainArmy.GetForce());

            DrawArmyData(Context._mainArmy);

            var remainingArmy = _unitsController.Execute(new HashSet<Unit>(Context._mainArmy));

            // TODO GD Turn Attack into an IUnitsControl
            Attack(Context._target, remainingArmy);

            Rally(_mapAnalyzer.GetClosestWalkable(Context._mainArmy.GetCenter(), searchRadius: 3), GetSoldiersNotInMainArmy().ToList());
        }

        private void DrawArmyData(IReadOnlyCollection<Unit> soldiers) {
            if (!Debug || soldiers.Count <= 0) {
                return;
            }

            Program.GraphicalDebugger.AddTextGroup(
                new[]
                {
                    $"Force: {soldiers.GetForce()}",
                },
                worldPos: _mapAnalyzer.WithWorldHeight(_mapAnalyzer.GetClosestWalkable(soldiers.GetCenter(), searchRadius: 3).Translate(1f, 1f)).ToPoint());
        }

        private void Attack(Vector2 targetToAttack, IReadOnlyCollection<Unit> army) {
            if (army.Count <= 0) {
                return;
            }

            DrawAttackData(targetToAttack, army);

            var unitsToAttackWith = army.Where(unit => unit.IsIdle() || unit.IsMoving() || unit.IsAttacking() || unit.IsMineralWalking())
                .Where(unit => !unit.IsBurrowed)
                .Where(unit => unit.DistanceTo(targetToAttack) > AcceptableDistanceToTarget)
                .ToList();

            var armyLocation = _mapAnalyzer.GetClosestWalkable(army.GetCenter(), searchRadius: 3);
            var absoluteDistanceToTarget = armyLocation.DistanceTo(targetToAttack);

            if (!army.IsFighting()) {
                _stuckDetector.Tick(armyLocation);
            }
            else {
                _stuckDetector.Reset(armyLocation);
            }

            // Try to take down rocks
            if (_stuckDetector.IsStuck) {
                Logger.Warning("{0} army is stuck", Name);

                var closestRock = Controller.GetUnits(_unitsTracker.NeutralUnits, Units.Destructibles).MinBy(rock => rock.DistanceTo(armyLocation));
                if (closestRock != null) {
                    Logger.Info("{0} closest rock is {1:F2} units away", Name, closestRock.DistanceTo(armyLocation));
                    if (closestRock.DistanceTo(armyLocation) <= RocksDestructionRange) {
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
                    Logger.Warning("{0} disabling pathfinding for {1:F2} seconds", Name, _pathfindingLockDelay / TimeUtils.FramesPerSecond);
                    _pathfindingLock = Controller.Frame + _pathfindingLockDelay;
                    _pathfindingLockDelay = Math.Min(MaximumPathfindingLockDelay, (ulong)(_pathfindingLockDelay * 1.25));

                    _stuckDetector.Reset(armyLocation);
                }

                AttackMove(targetToAttack, unitsToAttackWith);
            }
        }

        private void DrawAttackData(Vector2 targetToAttack, IEnumerable<Unit> soldiers) {
            if (!Debug) {
                return;
            }

            Program.GraphicalDebugger.AddSphere(_mapAnalyzer.WithWorldHeight(targetToAttack), AcceptableDistanceToTarget, Colors.Red);
            Program.GraphicalDebugger.AddText("Attack", worldPos: _mapAnalyzer.WithWorldHeight(targetToAttack).ToPoint());
            foreach (var soldier in soldiers) {
                Program.GraphicalDebugger.AddLine(soldier.Position, _mapAnalyzer.WithWorldHeight(targetToAttack), Colors.Red);
            }
        }

        private void WalkAlongThePath(Vector2 targetToAttack, Vector2 armyLocation, IEnumerable<Unit> soldiers) {
            var path = Pathfinder.Instance.FindPath(armyLocation, targetToAttack);
            if (path != null && path.Count > 0) {
                targetToAttack = path[Math.Min(path.Count - 1, PathfindingStep)];
            }

            AttackMove(targetToAttack, soldiers);
        }

        private void AttackMove(Vector2 targetToAttack, IEnumerable<Unit> soldiers) {
            targetToAttack = _mapAnalyzer.GetClosestWalkable(targetToAttack);

            soldiers
                .ToList()
                .ForEach(unit => unit.AttackMove(targetToAttack));
        }

        private static void Attack(Unit targetToAttack, IEnumerable<Unit> soldiers) {
            soldiers
                .ToList()
                .ForEach(unit => unit.Attack(targetToAttack));
        }

        private void Rally(Vector2 rallyPoint, IReadOnlyCollection<Unit> soldiers) {
            if (soldiers.Count <= 0) {
                return;
            }

            rallyPoint = _mapAnalyzer.GetClosestWalkable(rallyPoint);

            DrawRallyData(rallyPoint, soldiers);

            AttackMove(rallyPoint, soldiers.Where(unit => unit.DistanceTo(rallyPoint) > AcceptableDistanceToTarget));
        }

        private void DrawRallyData(Vector2 rallyPoint, IEnumerable<Unit> soldiers) {
            if (!Debug) {
                return;
            }

            Program.GraphicalDebugger.AddSphere(_mapAnalyzer.WithWorldHeight(rallyPoint), AcceptableDistanceToTarget, Colors.Blue);
            Program.GraphicalDebugger.AddText("Rally", worldPos: _mapAnalyzer.WithWorldHeight(rallyPoint).ToPoint());
            foreach (var soldier in soldiers) {
                Program.GraphicalDebugger.AddLine(soldier.Position, _mapAnalyzer.WithWorldHeight(rallyPoint), Colors.Blue);
            }
        }

        private IEnumerable<Unit> GetSoldiersNotInMainArmy() {
            return Context.Army.Where(soldier => !Context._mainArmy.Contains(soldier));
        }
    }
}
