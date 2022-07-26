using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.GameData;

namespace Bot.Managers;

public partial class ArmyManager {
    public class HuntStrategy: IStrategy {
        private static Dictionary<Vector3, bool> _checkedLocations;

        private bool _isNextTargetSet = false;

        private readonly ArmyManager _armyManager;

        public HuntStrategy(ArmyManager armyManager) {
            _armyManager = armyManager;
        }

        public string Name => "Hunt";

        public bool CanTransition() {
            return _isNextTargetSet;
        }

        public IStrategy Transition() {
            return new AttackStrategy(_armyManager);
        }

        public void Execute() {
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
            _checkedLocations = MapAnalyzer.ExpandLocations.ToDictionary(expand => expand, VisibilityTracker.IsVisible);
        }

        private void AssignNewTarget() {
            var nextUncheckedLocation = MapAnalyzer.ExpandLocations
                .Where(expandLocation => !_checkedLocations[expandLocation])
                .OrderBy(expandLocation => _armyManager._target.DistanceTo(expandLocation))
                .FirstOrDefault();

            Logger.Info("HuntStrategy next target is: {0}", nextUncheckedLocation);
            _armyManager._target = nextUncheckedLocation;
            _isNextTargetSet = true;
        }
    }
}
