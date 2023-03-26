using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.StateManagement;
using Bot.UnitModules;
using Bot.Utils;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl.SneakAttack;

public partial class SneakAttackUnitsControl: IWatchUnitsDie, IUnitsControl {
    private const float TankRange = 13;

    private readonly StateMachine<SneakAttackUnitsControl, SneakAttackState> _stateMachine;
    private readonly HashSet<Unit> _unitsWithUninstalledModule = new HashSet<Unit>();
    private Vector2 _targetPosition;
    private bool _isTargetPriority = false;

    private ulong _coolDownUntil = 0;

    private List<Unit> _army;
    private Vector2 _armyCenter;

    private static readonly HashSet<uint> PriorityTargets = new HashSet<uint>
    {
        Units.SiegeTank,
        Units.SiegeTankSieged,
        Units.Colossus,
        Units.Immortal,
    };

    public SneakAttackUnitsControl() {
        _stateMachine = new StateMachine<SneakAttackUnitsControl, SneakAttackState>(this, new InactiveState());
    }

    public bool IsExecuting() {
        return _stateMachine.State is not InactiveState;
    }

    public IReadOnlySet<Unit> Execute(IReadOnlySet<Unit> army) {
        var effectiveArmy = Controller.GetUnits(army, Units.Roach).ToList();
        if (!IsViable(effectiveArmy)) {
            Reset(army);

            return army;
        }

        _coolDownUntil = Controller.Frame + TimeUtils.SecsToFrames(5);

        _army = effectiveArmy;
        if (_army.Count <= 0) {
            Logger.Error("Trying to execute sneak attack without roaches in the army");
            return army;
        }

        _armyCenter = _army.GetCenter();

        UninstallBurrowMicroModules();

        _stateMachine.OnFrame();

        DebugTarget();

        var uncontrolledUnits = new HashSet<Unit>(army);
        uncontrolledUnits.ExceptWith(_army);

        return uncontrolledUnits;
    }

    private bool IsViable(IReadOnlyCollection<Unit> army) {
        if (!HasProperTech()) {
            return false;
        }

        if (!DetectionTracker.IsStealthEffective()) {
            return false;
        }

        if (army.Count <= 0) {
            return false;
        }

        return _stateMachine.State.IsViable(army);
    }

    public void Reset(IReadOnlyCollection<Unit> army) {
        if (_stateMachine.State is InactiveState) {
            return;
        }

        foreach (var roach in _unitsWithUninstalledModule) {
            BurrowMicroModule.Install(roach);
            roach.RemoveDeathWatcher(this);
        }

        _targetPosition = default;
        _unitsWithUninstalledModule.Clear();

        _stateMachine.TransitionTo(new InactiveState());
    }

    public void ReportUnitDeath(Unit deadUnit) {
        _unitsWithUninstalledModule.Remove(deadUnit);
    }

    private static bool HasProperTech() {
        return Controller.ResearchedUpgrades.Contains(Upgrades.TunnelingClaws) && Controller.ResearchedUpgrades.Contains(Upgrades.Burrow);
    }

    private static bool IsArmyGettingEngaged(IEnumerable<Unit> army) {
        // TODO GD Track HP loss, EngagedTargetTag is only set on our units
        // TODO GD We will often get hit by the first tank shell / enemy salvo, take that into account
        return false;
    }

    private static IEnumerable<Unit> GetGroundEnemiesInSight(IReadOnlyCollection<Unit> army) {
        return Controller.GetUnits(UnitsTracker.EnemyUnits, Units.Military)
            .Where(enemy => enemy.IsVisible)
            .Where(enemy => !enemy.IsFlying)
            .Where(enemy => army.Any(soldier => enemy.DistanceTo(soldier) <= Math.Max(enemy.UnitTypeData.SightRange, soldier.UnitTypeData.SightRange)));
    }

    private static void BurrowOverlings(IEnumerable<Unit> army) {
        foreach (var soldier in army.Where(soldier => !soldier.IsBurrowed)) {
            soldier.UseAbility(Abilities.BurrowRoachDown);
        }
    }

    private static IEnumerable<Unit> GetPriorityTargetsInOperationRadius(IReadOnlyCollection<Unit> army, float operationRadius) {
        return Controller
            .GetUnits(UnitsTracker.EnemyUnits.Concat(UnitsTracker.EnemyGhostUnits.Values), PriorityTargets)
            .Where(enemy => army.Min(soldier => soldier.DistanceTo(enemy)) <= operationRadius);
    }

    private void DebugTarget() {
        if (_targetPosition == default) {
            return;
        }

        Program.GraphicalDebugger.AddLink(_armyCenter.ToVector3(), _targetPosition.ToVector3(), Colors.Magenta);
        Program.GraphicalDebugger.AddSphere(_targetPosition.ToVector3(), 1, Colors.Magenta);

        if (_isTargetPriority) {
            Program.GraphicalDebugger.AddText("!", size: 20, worldPos: _targetPosition.ToVector3().ToPoint());
        }
    }

    private void UninstallBurrowMicroModules() {
        foreach (var roach in _army.Where(roach => !_unitsWithUninstalledModule.Contains(roach))) {
            roach.AddDeathWatcher(this);
            _unitsWithUninstalledModule.Add(roach);
            UnitModule.Uninstall<BurrowMicroModule>(roach);
        }
    }
}
