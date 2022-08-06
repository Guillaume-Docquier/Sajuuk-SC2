using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.StateManagement;
using Bot.UnitModules;
using Bot.Wrapper;

namespace Bot.Managers.ArmyManagement.Tactics.SneakAttack;

public partial class SneakAttackTactic: StateMachine<SneakAttackState>, IWatchUnitsDie, ITactic {
    private const float TankRange = 13;
    private const float OperationRadius = TankRange + 2;

    private readonly HashSet<Unit> _unitsWithUninstalledModule = new HashSet<Unit>();
    private Vector3 _targetPosition;
    private bool _isTargetPriority = false;

    private ulong _coolDownUntil = 0;

    private List<Unit> _army;
    private Vector3 _armyCenter;

    private static readonly HashSet<uint> PriorityTargets = new HashSet<uint>
    {
        Units.SiegeTank,
        Units.SiegeTankSieged,
        Units.Colossus,
        Units.Immortal,
    };

    public SneakAttackTactic() : base(new InactiveState()) {}

    public bool IsViable(IReadOnlyCollection<Unit> army) {
        if (!DetectionTracker.IsStealthEffective()) {
            return false;
        }

        if (!HasProperTech()) {
            return false;
        }

        // TODO GD Make RoachBurrowed equivalent to Roach and use Controller.GetUnits
        var effectiveArmy = army.Where(soldier => soldier.UnitType is Units.Roach or Units.RoachBurrowed).ToList();
        if (effectiveArmy.Count <= 0) {
            return false;
        }

        return State.IsViable(effectiveArmy);
    }

    public bool IsExecuting() {
        return State is not InactiveState;
    }

    public void Execute(IReadOnlyCollection<Unit> army) {
        Controller.FrameDelayMs = Controller.RealTime;
        _coolDownUntil = Controller.Frame + Controller.SecsToFrames(5);

        _army = army.Where(soldier => soldier.UnitType is Units.Roach or Units.RoachBurrowed).ToList();
        if (_army.Count <= 0) {
            Logger.Error("Trying to execute sneak attack without roaches in the army");
            return;
        }

        _armyCenter = army.GetCenter();

        foreach (var roach in _army.Where(roach => !_unitsWithUninstalledModule.Contains(roach))) {
            roach.AddDeathWatcher(this);
            _unitsWithUninstalledModule.Add(roach);
            UnitModule.Uninstall<BurrowMicroModule>(roach);
        }

        State.OnFrame();

        if (_targetPosition != default) {
            GraphicalDebugger.AddLink(_targetPosition, _armyCenter, Colors.Magenta);
            GraphicalDebugger.AddSphere(_targetPosition, 1, Colors.Magenta);

            if (_isTargetPriority) {
                GraphicalDebugger.AddText("!", size: 20, worldPos: _targetPosition.ToPoint());
            }
        }
    }

    public void Reset(IReadOnlyCollection<Unit> army) {
        if (State is InactiveState) {
            return;
        }

        foreach (var roach in _unitsWithUninstalledModule) {
            BurrowMicroModule.Install(roach);
            roach.RemoveDeathWatcher(this);
        }

        _targetPosition = default;
        _unitsWithUninstalledModule.Clear();

        TransitionTo(new InactiveState());
    }

    public void ReportUnitDeath(Unit deadUnit) {
        _unitsWithUninstalledModule.Remove(deadUnit);
    }

    private static bool HasProperTech() {
        return Controller.ResearchedUpgrades.Contains(Upgrades.TunnelingClaws) && Controller.ResearchedUpgrades.Contains(Upgrades.Burrow);
    }

    private static bool IsArmyGettingEngaged(IEnumerable<Unit> army) {
        // TODO GD Track HP loss, EngagedTargetTag is only set on our units
        return false;
    }

    private static IEnumerable<Unit> GetGroundEnemiesInSight(IReadOnlyCollection<Unit> army) {
        return Controller.GetUnits(UnitsTracker.EnemyUnits, Units.Military)
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
        return Controller.GetUnits(UnitsTracker.EnemyUnits, PriorityTargets).Where(enemy => enemy.HorizontalDistanceTo(armyCenter) <= OperationRadius);
    }

    private static IEnumerable<Unit> GetArmyWithEnoughHealth(IEnumerable<Unit> army) {
        return army.Where(soldier => soldier.Integrity > BurrowMicroModule.BurrowDownThreshold);
    }
}
