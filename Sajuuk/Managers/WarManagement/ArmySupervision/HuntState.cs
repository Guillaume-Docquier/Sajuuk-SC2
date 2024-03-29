﻿using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sajuuk.ExtensionMethods;
using Sajuuk.GameSense;
using Sajuuk.MapAnalysis;
using Sajuuk.StateManagement;

namespace Sajuuk.Managers.WarManagement.ArmySupervision;

public partial class ArmySupervisor {
    public class HuntState: State<ArmySupervisor> {
        private readonly IVisibilityTracker _visibilityTracker;
        private readonly IUnitsTracker _unitsTracker;
        private readonly ITerrainTracker _terrainTracker;
        private readonly IRegionsTracker _regionsTracker;
        private readonly IArmySupervisorStateFactory _armySupervisorStateFactory;
        private readonly IPathfinder _pathfinder;

        private static Dictionary<Vector2, bool> _checkedExpandLocations;
        private static readonly Dictionary<Vector2, bool> CheckedPositions = new Dictionary<Vector2, bool>();

        private bool _isNextTargetSet = false;

        public HuntState(
            IVisibilityTracker visibilityTracker,
            IUnitsTracker unitsTracker,
            ITerrainTracker terrainTracker,
            IRegionsTracker regionsTracker,
            IArmySupervisorStateFactory armySupervisorStateFactory,
            IPathfinder pathfinder
        ) {
            _visibilityTracker = visibilityTracker;
            _unitsTracker = unitsTracker;
            _terrainTracker = terrainTracker;
            _regionsTracker = regionsTracker;
            _armySupervisorStateFactory = armySupervisorStateFactory;
            _pathfinder = pathfinder;
        }

        protected override bool TryTransitioning() {
            if (_isNextTargetSet) {
                StateMachine.TransitionTo(_armySupervisorStateFactory.CreateAttackState());
                return true;
            }

            return false;
        }

        protected override void Execute() {
            if (_checkedExpandLocations == null) {
                ResetCheckedExpandLocations();
            }

            if (_checkedExpandLocations == null) {
                return;
            }

            foreach (var locationToCheck in _checkedExpandLocations.Keys) {
                _checkedExpandLocations[locationToCheck] |= _visibilityTracker.IsVisible(locationToCheck);
            }

            if (AllLocationsHaveBeenChecked(_checkedExpandLocations)) {
                ResetCheckedExpandLocations();
                if (AllLocationsHaveBeenChecked(_checkedExpandLocations)) {
                    var enemiesToAttack = _unitsTracker.EnemyUnits
                        .Where(unit => !unit.IsCloaked)
                        .Where(unit => Context.CanHitAirUnits || !unit.IsFlying)
                        .ToList();

                    if (enemiesToAttack.Count > 0) {
                        Context._target = enemiesToAttack[0].Position.ToVector2();
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

        private static bool AllLocationsHaveBeenChecked(Dictionary<Vector2, bool> locations) {
            return locations.Values.All(isChecked => isChecked);
        }

        private void ResetCheckedExpandLocations() {
            _checkedExpandLocations = _regionsTracker.ExpandLocations
                .Select(expandLocation => expandLocation.Position)
                .ToDictionary(expand => expand, _visibilityTracker.IsVisible);
        }

        private void ResetCheckedPositions() {
            CheckedPositions.Clear();
            for (var x = 0; x < _terrainTracker.MaxX; x++) {
                for (var y = 0; y < _terrainTracker.MaxY; y++) {
                    var position = new Vector2(x, y).AsWorldGridCenter();
                    if (!Context.CanFly && !_terrainTracker.IsWalkable(position)) {
                        continue;
                    }

                    CheckedPositions[position] = _visibilityTracker.IsVisible(position);
                }
            }
        }

        /// <summary>
        /// Check every corner of the map in search of enemies
        /// </summary>
        private void FindEnemies() {
            foreach (var positionToCheck in CheckedPositions.Where(kv => !kv.Value).Select(kv => kv.Key)) {
                CheckedPositions[positionToCheck] = _visibilityTracker.IsVisible(positionToCheck);
            }

            if (AllLocationsHaveBeenChecked(CheckedPositions)) {
                ResetCheckedPositions();
            }

            var armyCenter = _terrainTracker.GetClosestWalkable(Context._mainArmy.GetCenter(), searchRadius: 3);
            if (!Context.CanFly) {
                armyCenter = _terrainTracker.GetClosestWalkable(armyCenter);
            }

            var notVisiblePositions = CheckedPositions
                .Where(kv => !kv.Value)
                .Select(kv => kv.Key)
                .ToList();

            if (!notVisiblePositions.Any()) {
                return;
            }

            var closestNotVisiblePosition = notVisiblePositions.MinBy(position => position.DistanceTo(armyCenter));
            foreach (var unit in Context.Army) {
                unit.Move(closestNotVisiblePosition);
            }
        }

        private void AssignNewTarget() {
            // TODO GD Handle this better
            // We do this to break rocks because sometimes some locations will be unreachable
            // We should know about it, but the simplest way to fix this is to break the rocks and get on with it
            // TODO GD The module and the manager are giving orders to the unit, freezing it
            // _armyManager.Army.ForEach(TargetNeutralUnitsModule.Install);

            var armyCenter = _terrainTracker.GetClosestWalkable(Context._mainArmy.GetCenter());
            var nextReachableUncheckedLocations = _regionsTracker.ExpandLocations
                .Select(expandLocation => expandLocation.Position)
                .Where(expandLocation => !_checkedExpandLocations[expandLocation])
                .Where(expandLocation => _pathfinder.FindPath(armyCenter, expandLocation) != null)
                .ToList();

            if (nextReachableUncheckedLocations.Count == 0) {
                Logger.Info("<HuntState> All reachable expands have been checked, resetting search");
                ResetCheckedExpandLocations();
            }
            else {
                var nextTarget = nextReachableUncheckedLocations.MinBy(expandLocation => _pathfinder.FindPath(armyCenter, expandLocation).Count);
                Logger.Info("<HuntState> next target is: {0}", nextTarget);
                Context._target = nextTarget;
                _isNextTargetSet = true;
            }
        }
    }
}
