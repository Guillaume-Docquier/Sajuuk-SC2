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

namespace Bot.Managers.ArmySupervision.Tactics.SneakAttack;

public partial class SneakAttackTactic: IWatchUnitsDie, ITactic {
    private const float TankRange = 13;
    private const float OperationRadius = TankRange + 2;

    private readonly StateMachine<SneakAttackTactic, SneakAttackState> _stateMachine;
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

    public SneakAttackTactic() {
        _stateMachine = new StateMachine<SneakAttackTactic, SneakAttackState>(this, new InactiveState());
    }

    public bool IsViable(IReadOnlyCollection<Unit> army) {
        if (!HasProperTech()) {
            return false;
        }

        if (!DetectionTracker.IsStealthEffective()) {
            return false;
        }

        var effectiveArmy = Controller.GetUnits(army, Units.Roach).ToList();
        if (effectiveArmy.Count <= 0) {
            return false;
        }

        return _stateMachine.State.IsViable(effectiveArmy);
    }

    public bool IsExecuting() {
        return _stateMachine.State is not InactiveState;
    }

    public void Execute(IReadOnlyCollection<Unit> army) {
        _coolDownUntil = Controller.Frame + Controller.SecsToFrames(5);

        _army = army.Where(soldier => soldier.UnitType is Units.Roach or Units.RoachBurrowed).ToList();
        if (_army.Count <= 0) {
            Logger.Error("Trying to execute sneak attack without roaches in the army");
            return;
        }

        _armyCenter = army.GetCenter();

        UninstallBurrowMicroModules();

        _stateMachine.OnFrame();

        DebugTarget();
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
        return false;
    }

    private static IEnumerable<Unit> GetGroundEnemiesInSight(IReadOnlyCollection<Unit> army) {
        return Controller.GetUnits(UnitsTracker.EnemyUnits, Units.Military)
            .Where(enemy => enemy.IsVisible)
            .Where(enemy => !enemy.IsFlying)
            .Where(enemy => army.Any(soldier => enemy.HorizontalDistanceTo(soldier) <= Math.Max(enemy.UnitTypeData.SightRange, soldier.UnitTypeData.SightRange)));
    }

    private static void BurrowOverlings(IEnumerable<Unit> army) {
        foreach (var soldier in army.Where(soldier => !soldier.IsBurrowed)) {
            soldier.UseAbility(Abilities.BurrowRoachDown);
        }
    }

    private static IEnumerable<Unit> GetPriorityTargetsInOperationRadius(Vector3 armyCenter) {
        return Controller.GetUnits(UnitsTracker.EnemyUnits, PriorityTargets).Where(enemy => enemy.HorizontalDistanceTo(armyCenter) <= OperationRadius);
    }

    private void DebugTarget() {
        if (_targetPosition == default) {
            return;
        }

        Program.GraphicalDebugger.AddLink(_armyCenter, _targetPosition, Colors.Magenta);
        Program.GraphicalDebugger.AddSphere(_targetPosition, 1, Colors.Magenta);

        if (_isTargetPriority) {
            Program.GraphicalDebugger.AddText("!", size: 20, worldPos: _targetPosition.ToPoint());
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
