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
using Bot.MapAnalysis;
using Bot.StateManagement;
using Bot.Utils;

namespace Bot.Managers.WarManagement.ArmySupervision;

public partial class ArmySupervisor {
    public class AttackState: State<ArmySupervisor> {
        private const bool Debug = true;

        private readonly IUnitsTracker _unitsTracker;
        private readonly ITerrainTracker _terrainTracker;
        private readonly IGraphicalDebugger _graphicalDebugger;
        private readonly IArmySupervisorStateFactory _armySupervisorStateFactory;
        private readonly IFrameClock _frameClock;
        private readonly IUnitEvaluator _unitEvaluator;
        private readonly IPathfinder _pathfinder;

        private const float RocksDestructionRange = 9f;
        private const float AcceptableDistanceToTarget = 3;
        private const float MaxDistanceForPathfinding = 25;
        private const int PathfindingStep = 3;

        private readonly StuckDetector _stuckDetector = new StuckDetector();

        private static readonly ulong MaximumPathfindingLockDelay = TimeUtils.SecsToFrames(15);
        private bool PathfindingIsUnlocked => _pathfindingLock < _frameClock.CurrentFrame;
        private ulong _pathfindingLock = 0;
        private ulong _pathfindingLockDelay = TimeUtils.SecsToFrames(4);

        private readonly IUnitsControl _unitsController;

        public AttackState(
            IUnitsTracker unitsTracker,
            ITerrainTracker terrainTracker,
            IGraphicalDebugger graphicalDebugger,
            IArmySupervisorStateFactory armySupervisorStateFactory,
            IUnitsControlFactory unitsControlFactory,
            IFrameClock frameClock,
            IUnitEvaluator unitEvaluator,
            IPathfinder pathfinder
        ) {
            _unitsTracker = unitsTracker;
            _terrainTracker = terrainTracker;
            _graphicalDebugger = graphicalDebugger;
            _armySupervisorStateFactory = armySupervisorStateFactory;
            _frameClock = frameClock;
            _unitEvaluator = unitEvaluator;
            _pathfinder = pathfinder;

            _unitsController = unitsControlFactory.CreateOffensiveUnitsControl();
        }

        protected override void OnTransition() {
            _unitsController.Reset(ImmutableList<Unit>.Empty);
        }

        protected override bool TryTransitioning() {
            if (_unitsController.IsExecuting()) {
                return false;
            }

            if (_terrainTracker.GetClosestWalkable(Context._mainArmy.GetCenter(), searchRadius: 3).DistanceTo(Context._target) < AcceptableDistanceToTarget) {
                StateMachine.TransitionTo(_armySupervisorStateFactory.CreateDefenseState());
                return true;
            }

            return false;
        }

        protected override void Execute() {
            Context._strongestForce = Math.Max(Context._strongestForce, _unitEvaluator.EvaluateForce(Context._mainArmy));

            DrawArmyData(Context._mainArmy);

            var remainingArmy = _unitsController.Execute(new HashSet<Unit>(Context._mainArmy));

            // TODO GD Turn Attack into an IUnitsControl
            Attack(Context._target, remainingArmy);

            Rally(_terrainTracker.GetClosestWalkable(Context._mainArmy.GetCenter(), searchRadius: 3), GetSoldiersNotInMainArmy().ToList());
        }

        private void DrawArmyData(IReadOnlyCollection<Unit> soldiers) {
            if (!Debug || soldiers.Count <= 0) {
                return;
            }

            _graphicalDebugger.AddTextGroup(
                new[]
                {
                    $"Force: {_unitEvaluator.EvaluateForce(soldiers)}",
                },
                worldPos: _terrainTracker.WithWorldHeight(_terrainTracker.GetClosestWalkable(soldiers.GetCenter(), searchRadius: 3).Translate(1f, 1f)).ToPoint());
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

            var armyLocation = _terrainTracker.GetClosestWalkable(army.GetCenter(), searchRadius: 3);
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

                var closestRock = _unitsTracker.GetUnits(_unitsTracker.NeutralUnits, Units.Destructibles).MinBy(rock => rock.DistanceTo(armyLocation));
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
                    _pathfindingLock = _frameClock.CurrentFrame + _pathfindingLockDelay;
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

            _graphicalDebugger.AddSphere(_terrainTracker.WithWorldHeight(targetToAttack), AcceptableDistanceToTarget, Colors.Red);
            _graphicalDebugger.AddText("Attack", worldPos: _terrainTracker.WithWorldHeight(targetToAttack).ToPoint());
            foreach (var soldier in soldiers) {
                _graphicalDebugger.AddLine(soldier.Position, _terrainTracker.WithWorldHeight(targetToAttack), Colors.Red);
            }
        }

        private void WalkAlongThePath(Vector2 targetToAttack, Vector2 armyLocation, IEnumerable<Unit> soldiers) {
            var path = _pathfinder.FindPath(armyLocation, targetToAttack);
            if (path != null && path.Count > 0) {
                targetToAttack = path[Math.Min(path.Count - 1, PathfindingStep)];
            }

            AttackMove(targetToAttack, soldiers);
        }

        private void AttackMove(Vector2 targetToAttack, IEnumerable<Unit> soldiers) {
            targetToAttack = _terrainTracker.GetClosestWalkable(targetToAttack);

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

            rallyPoint = _terrainTracker.GetClosestWalkable(rallyPoint);

            DrawRallyData(rallyPoint, soldiers);

            AttackMove(rallyPoint, soldiers.Where(unit => unit.DistanceTo(rallyPoint) > AcceptableDistanceToTarget));
        }

        private void DrawRallyData(Vector2 rallyPoint, IEnumerable<Unit> soldiers) {
            if (!Debug) {
                return;
            }

            _graphicalDebugger.AddSphere(_terrainTracker.WithWorldHeight(rallyPoint), AcceptableDistanceToTarget, Colors.Blue);
            _graphicalDebugger.AddText("Rally", worldPos: _terrainTracker.WithWorldHeight(rallyPoint).ToPoint());
            foreach (var soldier in soldiers) {
                _graphicalDebugger.AddLine(soldier.Position, _terrainTracker.WithWorldHeight(rallyPoint), Colors.Blue);
            }
        }

        private IEnumerable<Unit> GetSoldiersNotInMainArmy() {
            return Context.Army.Where(soldier => !Context._mainArmy.Contains(soldier));
        }
    }
}
