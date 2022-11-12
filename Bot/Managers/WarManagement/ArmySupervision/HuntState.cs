using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.MapKnowledge;
using Bot.StateManagement;

namespace Bot.Managers.ArmySupervision;

public partial class ArmySupervisor {
    public class HuntState: State<ArmySupervisor> {
        private static Dictionary<Vector2, bool> _checkedExpandLocations;
        private static readonly Dictionary<Vector2, bool> CheckedPositions = new Dictionary<Vector2, bool>();

        private bool _isNextTargetSet = false;

        protected override bool TryTransitioning() {
            if (_isNextTargetSet) {
                StateMachine.TransitionTo(new AttackState());
                return true;
            }

            return false;
        }

        protected override void Execute() {
            if (MapAnalyzer.IsInitialized && _checkedExpandLocations == null) {
                ResetCheckedExpandLocations();
            }

            if (_checkedExpandLocations == null) {
                return;
            }

            foreach (var locationToCheck in _checkedExpandLocations.Keys) {
                _checkedExpandLocations[locationToCheck] |= VisibilityTracker.IsVisible(locationToCheck);
            }

            if (AllLocationsHaveBeenChecked(_checkedExpandLocations)) {
                ResetCheckedExpandLocations();
                if (AllLocationsHaveBeenChecked(_checkedExpandLocations)) {
                    var enemiesToAttack = UnitsTracker.EnemyUnits
                        .Where(unit => !unit.IsCloaked)
                        .Where(unit => StateMachine.Context._canHitAirUnits || !unit.IsFlying)
                        .ToList();

                    if (enemiesToAttack.Count > 0) {
                        StateMachine.Context._target = enemiesToAttack[0].Position.ToVector2();
                        _isNextTargetSet = true;
                    }
                    else {
                        FindEnemies();
                    }

                    return;
                }
            }

            AssignNewTarget();
        }

        private static bool AllLocationsHaveBeenChecked(Dictionary<Vector3, bool> locations) {
            return locations.Values.All(isChecked => isChecked);
        }

        private static bool AllLocationsHaveBeenChecked(Dictionary<Vector2, bool> locations) {
            return locations.Values.All(isChecked => isChecked);
        }

        private static void ResetCheckedExpandLocations() {
            _checkedExpandLocations = ExpandAnalyzer.ExpandLocations
                .Select(expandLocation => expandLocation.Position)
                .ToDictionary(expand => expand, VisibilityTracker.IsVisible);
        }

        private void ResetCheckedPositions() {
            CheckedPositions.Clear();
            for (var x = 0; x < MapAnalyzer.MaxX; x++) {
                for (var y = 0; y < MapAnalyzer.MaxY; y++) {
                    var position = new Vector2(x, y).AsWorldGridCenter();
                    if (!StateMachine.Context._canFly && !MapAnalyzer.IsWalkable(position)) {
                        continue;
                    }

                    CheckedPositions[position] = VisibilityTracker.IsVisible(position);
                }
            }
        }

        /// <summary>
        /// Check every corner of the map in search of enemies
        /// </summary>
        private void FindEnemies() {
            foreach (var positionToCheck in CheckedPositions.Where(kv => !kv.Value).Select(kv => kv.Key)) {
                CheckedPositions[positionToCheck] = VisibilityTracker.IsVisible(positionToCheck);
            }

            if (AllLocationsHaveBeenChecked(CheckedPositions)) {
                ResetCheckedPositions();
            }

            var armyCenter = StateMachine.Context._mainArmy.GetCenter();
            if (!StateMachine.Context._canFly) {
                armyCenter = armyCenter.ClosestWalkable();
            }

            var notVisiblePositions = CheckedPositions
                .Where(kv => !kv.Value)
                .Select(kv => kv.Key)
                .ToList();

            if (!notVisiblePositions.Any()) {
                return;
            }

            var closestNotVisiblePosition = notVisiblePositions.MinBy(position => position.DistanceTo(armyCenter));
            foreach (var unit in StateMachine.Context.Army) {
                unit.Move(closestNotVisiblePosition);
            }
        }

        private void AssignNewTarget() {
            // TODO GD Handle this better
            // We do this to break rocks because sometimes some locations will be unreachable
            // We should know about it, but the simplest way to fix this is to break the rocks and get on with it
            // TODO GD The module and the manager are giving orders to the unit, freezing it
            // _armyManager.Army.ForEach(TargetNeutralUnitsModule.Install);

            var armyCenter = StateMachine.Context._mainArmy.GetCenter().ClosestWalkable();
            var nextReachableUncheckedLocations = ExpandAnalyzer.ExpandLocations
                .Select(expandLocation => expandLocation.Position)
                .Where(expandLocation => !_checkedExpandLocations[expandLocation])
                .Where(expandLocation => Pathfinder.FindPath(armyCenter, expandLocation) != null)
                .ToList();

            if (nextReachableUncheckedLocations.Count == 0) {
                Logger.Info("<HuntState> All reachable expands have been checked, resetting search");
                ResetCheckedExpandLocations();
            }
            else {
                var nextTarget = nextReachableUncheckedLocations.MinBy(expandLocation => Pathfinder.FindPath(armyCenter, expandLocation).Count);
                Logger.Info("<HuntState> next target is: {0}", nextTarget);
                StateMachine.Context._target = nextTarget;
                _isNextTargetSet = true;
            }
        }
    }
}
