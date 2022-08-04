using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.MapKnowledge;
using Bot.StateManagement;

namespace Bot.Managers.ArmyManagement;

public partial class ArmyManager {
    public class HuntState: State<ArmyManager> {
        private static Dictionary<Vector3, bool> _checkedLocations;

        private bool _isNextTargetSet = false;

        protected override bool TryTransitioning() {
            if (_isNextTargetSet) {
                StateMachine.TransitionTo(new AttackState());
                return true;
            }

            return false;
        }

        protected override void Execute() {
            if (MapAnalyzer.IsInitialized && _checkedLocations == null) {
                InitCheckedLocations();
            }

            if (_checkedLocations == null) {
                return;
            }

            foreach (var locationToCheck in _checkedLocations.Keys) {
                _checkedLocations[locationToCheck] |= VisibilityTracker.IsVisible(locationToCheck);
            }

            if (AllLocationsHaveBeenChecked()) {
                InitCheckedLocations();
            }

            AssignNewTarget();
        }

        private static bool AllLocationsHaveBeenChecked() {
            return _checkedLocations.Values.All(isChecked => isChecked);
        }

        private static void InitCheckedLocations() {
            _checkedLocations = ExpandAnalyzer.ExpandLocations.ToDictionary(expand => expand, VisibilityTracker.IsVisible);
        }

        private void AssignNewTarget() {
            // TODO GD Handle this better
            // We do this to break rocks because sometimes some locations will be unreachable
            // We should know about it, but the simplest way to fix this is to break the rocks and get on with it
            // TODO GD The module and the manager are giving orders to the unit, freezing it
            // _armyManager.Army.ForEach(TargetNeutralUnitsModule.Install);

            var armyCenter = StateMachine._mainArmy.GetCenter().ClosestWalkable();
            var nextUncheckedLocation = ExpandAnalyzer.ExpandLocations
                .Where(expandLocation => !_checkedLocations[expandLocation])
                .Where(expandLocation => Pathfinder.FindPath(armyCenter, expandLocation) != null)
                .MinBy(expandLocation => Pathfinder.FindPath(armyCenter, expandLocation).Count);

            if (nextUncheckedLocation == default) {
                Logger.Info("<HuntStrategy> could not find a next target, resetting search");
                InitCheckedLocations();
            }
            else {
                Logger.Info("<HuntStrategy> next target is: {0}", nextUncheckedLocation);
                StateMachine._target = nextUncheckedLocation;
                _isNextTargetSet = true;
            }
        }
    }
}
