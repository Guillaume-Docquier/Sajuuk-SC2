using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.GameData;
using Bot.GameSense;
using Bot.UnitModules;
using Bot.Wrapper;

namespace Bot.Managers.ArmyManagement.Tactics;

public class BurrowSurpriseTactic: IWatchUnitsDie, ITactic {
    private enum State {
        None,
        Approach,
        Setup,
        Engage,
        Fight,
    }

    private const float SetupDistance = 1.25f;
    private const float EngageDistance = 0.75f;

    private const float TankRange = 13;
    private const float OperationRadius = TankRange + 2;

    private readonly HashSet<Unit> _unitsWithUninstalledModule = new HashSet<Unit>();
    private State _state = State.None;
    private Vector3 _targetPosition;
    private bool _isTargetPriority = false;

    private ulong _coolDownUntil = 0;

    private static readonly HashSet<uint> PriorityTargets = new HashSet<uint>
    {
        Units.SiegeTank,
        Units.SiegeTankSieged,
        Units.Colossus,
        Units.Immortal,
    };

    public bool IsViable(IReadOnlyCollection<Unit> army) {
        army = army.Where(soldier => soldier.UnitType is Units.Roach or Units.RoachBurrowed).ToList();

        if (!DetectionTracker.IsStealthEffective()) {
            return false;
        }

        if (_state == State.None) {
            if (_coolDownUntil > Controller.Frame) {
                return false;
            }

            if (!HasProperTech()) {
                return false;
            }

            if (IsArmyDetected(army)) {
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

            return enemiesInSight.MinBy(enemy => enemy.HorizontalDistanceTo(armyCenter))!.HorizontalDistanceTo(armyCenter) >= maxSightRange / 2;
        }

        if (_state is State.Approach or State.Setup) {
            if (IsArmyDetected(army)) {
                return false;
            }

            // If we're engaged, it means they somehow see us, abort!
            return !IsArmyGettingEngaged(army);
        }

        return _state != State.Fight;
    }

    public bool IsExecuting() {
        return _state != State.None;
    }

    public void Execute(IReadOnlyCollection<Unit> army) {
        army = army.Where(soldier => soldier.UnitType is Units.Roach or Units.RoachBurrowed).ToList();

        _coolDownUntil = Controller.Frame + Controller.SecsToFrames(5);
        Controller.FrameDelayMs = Controller.RealTime;

        var armyCenter = army.GetCenter();
        GraphicalDebugger.AddSphere(armyCenter, 1, Colors.Magenta);

        if (_state == State.None) {
            foreach (var roach in army.Where(roach => !_unitsWithUninstalledModule.Contains(roach))) {
                roach.AddDeathWatcher(this);
                _unitsWithUninstalledModule.Add(roach);
                UnitModule.Uninstall<BurrowMicroModule>(roach);
            }

            BurrowOverlings(army);

            var closestPriorityTarget = GetPriorityTargetsInOperationRadius(armyCenter).MinBy(enemy => enemy.HorizontalDistanceTo(armyCenter));
            if (closestPriorityTarget != null) {
                _targetPosition = closestPriorityTarget.Position;
                _isTargetPriority = true;
            }
            else {
                var closestEnemy = GetGroundEnemiesInSight(army).MinBy(enemy => enemy.HorizontalDistanceTo(armyCenter));
                if (closestEnemy == null) {
                    Logger.Error("BurrowSurprise: Went from None -> Fight because no enemies nearby");
                    _state = State.Fight;
                    return;
                }

                _targetPosition = closestEnemy.Position;
            }

            _state = State.Approach;
        } else if (_state == State.Approach) {
            var closestPriorityTarget = GetPriorityTargetsInOperationRadius(armyCenter).MinBy(enemy => enemy.HorizontalDistanceTo(armyCenter));
            if (closestPriorityTarget != null) {
                _targetPosition = closestPriorityTarget.Position;
                _isTargetPriority = true;
            }
            else if (!_isTargetPriority) {
                var closestVisibleEnemy = GetGroundEnemiesInSight(army).MinBy(enemy => enemy.HorizontalDistanceTo(armyCenter));
                if (closestVisibleEnemy != null) {
                    _targetPosition = closestVisibleEnemy.Position;
                    _isTargetPriority = false;
                }
            }

            if (_targetPosition == default) {
                Logger.Warning("BurrowSurprise: Went from Approach -> Fight because _targetPosition == default");
                _isTargetPriority = false;
                _state = State.Fight;
                return;
            }

            if (_targetPosition.HorizontalDistanceTo(armyCenter) > SetupDistance) {
                BurrowOverlings(army);

                foreach (var soldier in army.Where(soldier => soldier.IsIdleOrMovingOrAttacking())) {
                    soldier.Move(_targetPosition);
                }
            }
            else {
                _targetPosition = default;
                _isTargetPriority = false;
                _state = State.Setup;
            }
        } else if (_state == State.Setup) {
            // Do we need _isTargetPriority at this point? We shouldn't lose sight at this point, right?
            var closestPriorityTarget = GetPriorityTargetsInOperationRadius(armyCenter).MinBy(enemy => enemy.HorizontalDistanceTo(armyCenter));
            if (closestPriorityTarget != null) {
                _targetPosition = closestPriorityTarget.Position;
                _isTargetPriority = true;
            }
            else {
                var enemies = Controller.GetUnits(Controller.EnemyUnits, Units.Military).ToList();
                var closestEnemyCluster = Clustering.DBSCAN(enemies, 5, 2).MinBy(cluster => cluster.GetCenter().HorizontalDistanceTo(armyCenter));
                if (closestEnemyCluster != null && armyCenter.HorizontalDistanceTo(closestEnemyCluster.GetCenter()) > OperationRadius) {
                    _targetPosition = closestEnemyCluster.GetCenter();
                    _isTargetPriority = false;
                }
            }

            if (_targetPosition == default) {
                Logger.Warning("BurrowSurprise: Went from Setup -> Fight because _targetPosition == default");
                _state = State.Fight;
            }
            else {
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

        if (_targetPosition != default) {
            GraphicalDebugger.AddLine(_targetPosition, armyCenter, Colors.Magenta);
            GraphicalDebugger.AddSphere(_targetPosition, 1, Colors.Magenta);

            if (_isTargetPriority) {
                GraphicalDebugger.AddText("!", size: 20, worldPos: _targetPosition.ToPoint());
            }
        }
    }

    public void Reset(IReadOnlyCollection<Unit> army) {
        if (_state == State.None) {
            return;
        }

        foreach (var roach in _unitsWithUninstalledModule) {
            if (roach.RawUnitData.IsBurrowed) {
                roach.UseAbility(Abilities.BurrowRoachUp);
            }

            BurrowMicroModule.Install(roach);
        }

        _state = State.None;
        _targetPosition = default;
        _unitsWithUninstalledModule.Clear();
    }

    public void ReportUnitDeath(Unit deadUnit) {
        _unitsWithUninstalledModule.Remove(deadUnit);
    }

    private static bool HasProperTech() {
        return Controller.ResearchedUpgrades.Contains(Upgrades.TunnelingClaws) && Controller.ResearchedUpgrades.Contains(Upgrades.Burrow);
    }

    private static bool IsArmyDetected(IReadOnlyCollection<Unit> army) {
        return IsArmyScanned(army) || GetDetectorsThatCanSee(army).Any();
    }

    private static bool IsArmyScanned(IReadOnlyCollection<Unit> army) {
        var scanRadius = KnowledgeBase.GetEffectData(Effects.ScanSweep).Radius;

        return Controller.GetEffects(Effects.ScanSweep)
            .SelectMany(scanEffect => scanEffect.Pos.ToList())
            .Any(scan => army.Any(soldier => scan.ToVector3().HorizontalDistanceTo(soldier.Position) <= scanRadius));
    }

    private static IEnumerable<Unit> GetDetectorsThatCanSee(IReadOnlyCollection<Unit> army) {
        return Controller.GetUnits(Controller.EnemyUnits, Units.Detectors)
            .Where(detector => army.Any(soldier => soldier.HorizontalDistanceTo(detector) <= detector.UnitTypeData.SightRange));
    }

    private static bool IsArmyGettingEngaged(IEnumerable<Unit> army) {
        // TODO GD Track HP loss, EngagedTargetTag is only set on our units
        return false;
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

    private static IEnumerable<Unit> GetPriorityTargetsInOperationRadius(Vector3 armyCenter) {
        return Controller.GetUnits(Controller.EnemyUnits, PriorityTargets).Where(enemy => enemy.HorizontalDistanceTo(armyCenter) <= OperationRadius);
    }
}
