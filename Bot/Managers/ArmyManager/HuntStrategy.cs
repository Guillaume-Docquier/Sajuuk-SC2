using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Bot.Managers;

public partial class ArmyManager {
    public class HuntStrategy: IStrategy {
        private static Dictionary<Vector3, bool> _checkedLocations;

        private bool _isNextTargetSet = false;

        private readonly ArmyManager _armyManager;

        public HuntStrategy(ArmyManager armyManager) {
            _armyManager = armyManager;
        }

        public bool CanTransition() {
            return _isNextTargetSet;
        }

        public IStrategy Transition() {
            return new AttackStrategy(_armyManager);
        }

        public void Execute() {
            if (MapAnalyzer.IsInitialized && _checkedLocations == null) {
                _checkedLocations = MapAnalyzer.ExpandLocations.ToDictionary(expand => expand, _ => false);
            }

            if (_checkedLocations == null) {
                return;
            }

            if (AllLocationsHaveBeenChecked()) {
                ResetCheckedLocations();
            }

            // TODO GD Check that we see it, don't rely on the fact that we reached the current state
            _checkedLocations[_armyManager._target] = true;

            AssignNewTarget();
        }

        private static bool AllLocationsHaveBeenChecked() {
            return _checkedLocations.Values.All(scouted => scouted);
        }

        private static void ResetCheckedLocations() {
            foreach (var expandLocation in _checkedLocations.Keys) {
                _checkedLocations[expandLocation] = false;
            }
        }

        private void AssignNewTarget() {
            var nextUncheckedLocation = MapAnalyzer.ExpandLocations
                .Where(expandLocation => !_checkedLocations[expandLocation])
                .OrderBy(expandLocation => _armyManager._target.DistanceTo(expandLocation))
                .FirstOrDefault();

            _armyManager._target = nextUncheckedLocation;
            _isNextTargetSet = true;
        }
    }
}
