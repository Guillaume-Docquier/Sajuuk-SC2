using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.GameData;
using Bot.UnitModules;

namespace Bot.Managers.ArmyManagement.Tactics;

public class BurrowSurpriseTactic: ITactic {
    private enum State {
        None,
        Approach,
        Setup,
        Engage,
        Fight,
    }

    private const float SetupDistance = 2;
    private const float EngageDistance = 1;

    private State _state = State.None;
    private Unit _lastEnemySeen;
    private Vector3 _targetPosition;

    private static HashSet<ulong> PriorityTargets = new HashSet<ulong>
    {
        Units.SiegeTank,
        Units.SiegeTankSieged,
        Units.Colossus,
        Units.Immortal,
    };

    public bool IsViable(IReadOnlyCollection<Unit> army) {
        if (_state == State.None) {
            if (!Controller.ResearchedUpgrades.Contains(Upgrades.TunnelingClaws) || !Controller.ResearchedUpgrades.Contains(Upgrades.Burrow)) {
                return false;
            }

            if (GetDetectorsThatCanSee(army).Any()) {
                return false;
            }

            var armyCenter = army.GetCenter();
            var maxSightRange = army.DistinctBy(soldier => soldier.UnitType)
                .Select(soldier => soldier.UnitTypeData.SightRange)
                .Max();

            var enemiesInSight = GetGroundEnemiesInSight(army).ToList();

            if (!enemiesInSight.Any()) {
                return false;
            }

            return enemiesInSight.MinBy(enemy => enemy.DistanceTo(armyCenter))!.DistanceTo(armyCenter) >= maxSightRange / 2;
        }

        if (_state is State.Approach or State.Setup) {
            if (_lastEnemySeen == null) {
                return false;
            }

            // If we're engaged, it means they see us, abort!
            return !GetEnemiesEngagingArmy(army).Any();
        }

        return _state != State.Fight;
    }

    public void Execute(IReadOnlyCollection<Unit> army) {
        var armyCenter = army.GetCenter();
        if (_state == State.None) {
            foreach (var roach in army.Where(soldier => soldier.UnitType is Units.Roach or Units.RoachBurrowed)) {
                UnitModule.Uninstall<BurrowMicroModule>(roach);
            }

            BurrowOverlings(army);

            _lastEnemySeen = GetGroundEnemiesInSight(army).MinBy(enemy => enemy.HorizontalDistanceTo(armyCenter));
            _state = State.Approach;
        } else if (_state == State.Approach) {
            var closestVisibleEnemy = GetGroundEnemiesInSight(army).MinBy(enemy => enemy.HorizontalDistanceTo(armyCenter));
            if (closestVisibleEnemy != null) {
                _lastEnemySeen = closestVisibleEnemy;
            }

            if (_lastEnemySeen.DistanceTo(armyCenter) > SetupDistance) {
                BurrowOverlings(army);

                foreach (var soldier in army.Where(soldier => soldier.IsIdleOrMovingOrAttacking())) {
                    soldier.Move(_lastEnemySeen.Position);
                }
            }
            else {
                _state = State.Setup;
            }
        } else if (_state == State.Setup) {
            // TODO GD Track priority targets in a radius of 13 (siege tank range)

            var enemies = Controller.GetUnits(Controller.EnemyUnits, Units.Military).ToList();
            var closestEnemyCluster = Clustering.DBSCAN(enemies, 5, 2).MinBy(cluster => cluster.GetCenter().HorizontalDistanceTo(armyCenter));
            if (closestEnemyCluster == null) {
                _state = State.Engage;
            }
            else {
                _targetPosition = closestEnemyCluster.GetCenter();
                if (_targetPosition.HorizontalDistanceTo(armyCenter) > EngageDistance) {
                    foreach (var soldier in army.Where(soldier => soldier.IsIdleOrMovingOrAttacking())) {
                        soldier.Move(_targetPosition);
                    }
                }
                else {
                    _state = State.Engage;
                }
            }
        } else if (_state == State.Engage) {
            UnburrowUnderlings(army);

            _state = State.Fight;
        }
    }

    public void Reset(IReadOnlyCollection<Unit> army) {
        if (_state == State.None) {
            return;
        }

        _state = State.None;
        _lastEnemySeen = null;

        UnburrowUnderlings(army);

        foreach (var roach in army.Where(soldier => soldier.UnitType is Units.Roach or Units.RoachBurrowed)) {
            BurrowMicroModule.Install(roach); // TODO Will this take over because of 'collisions'?
        }
    }

    private static IEnumerable<Unit> GetDetectorsThatCanSee(IReadOnlyCollection<Unit> army) {
        return Controller.GetUnits(Controller.EnemyUnits, Units.Detectors)
            .Where(detector => army.Any(soldier => soldier.HorizontalDistanceTo(detector) <= detector.UnitTypeData.SightRange));
    }

    private static IEnumerable<Unit> GetEnemiesEngagingArmy(IEnumerable<Unit> army) {
        var armyTags = new HashSet<ulong>(army.Select(soldier => soldier.Tag));

        return Controller.GetUnits(Controller.EnemyUnits, Units.Military).Where(enemy => enemy.IsAttacking(armyTags));
    }

    private static IEnumerable<Unit> GetGroundEnemiesInSight(IReadOnlyCollection<Unit> army) {
        return Controller.GetUnits(Controller.EnemyUnits, Units.Military)
            .Where(enemy => enemy.IsVisible)
            .Where(enemy => !enemy.RawUnitData.IsFlying)
            .Where(enemy => army.Any(soldier => enemy.HorizontalDistanceTo(soldier) <= Math.Max(enemy.UnitTypeData.SightRange, soldier.UnitTypeData.SightRange)));
    }

    private static void BurrowOverlings(IEnumerable<Unit> army) {
        foreach (var soldier in army.Where(soldier => !soldier.RawUnitData.IsBurrowed)) {
            soldier.UseAbility(Abilities.BurrowRoachDown);
        }
    }

    private static void UnburrowUnderlings(IEnumerable<Unit> army) {
        foreach (var soldier in army.Where(soldier => soldier.RawUnitData.IsBurrowed)) {
            soldier.UseAbility(Abilities.BurrowRoachUp);
        }
    }
}
