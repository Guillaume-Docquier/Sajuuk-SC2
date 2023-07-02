using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sajuuk.ExtensionMethods;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.StateManagement;
using Sajuuk.Utils;

namespace Sajuuk.Managers.WarManagement.ArmySupervision.UnitsControl.SneakAttackUnitsControl;

public partial class SneakAttack : IUnitsControl {
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IFrameClock _frameClock;
    private readonly IController _controller;
    private readonly IDetectionTracker _detectionTracker;
    private readonly ISneakAttackStateFactory _sneakAttackStateFactory;

    private const float TankRange = 13;

    private readonly StateMachine<SneakAttack, SneakAttackState> _stateMachine;
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

    public SneakAttack(
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IGraphicalDebugger graphicalDebugger,
        IFrameClock frameClock,
        IController controller,
        IDetectionTracker detectionTracker,
        ISneakAttackStateFactory sneakAttackStateFactory
    ) {
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _graphicalDebugger = graphicalDebugger;
        _frameClock = frameClock;
        _controller = controller;
        _detectionTracker = detectionTracker;
        _sneakAttackStateFactory = sneakAttackStateFactory;

        _stateMachine = new StateMachine<SneakAttack, SneakAttackState>(this, _sneakAttackStateFactory.CreateInactiveState());
    }

    public bool IsExecuting() {
        return _stateMachine.State is not InactiveState;
    }

    public IReadOnlySet<Unit> Execute(IReadOnlySet<Unit> army) {
        var effectiveArmy = _unitsTracker.GetUnits(army, Units.Roach).ToList();
        if (!IsViable(effectiveArmy)) {
            Reset(army);

            return army;
        }

        _coolDownUntil = _frameClock.CurrentFrame + TimeUtils.SecsToFrames(5);

        _army = effectiveArmy;
        if (_army.Count <= 0) {
            Logger.Error("Trying to execute sneak attack without roaches in the army");
            return army;
        }

        _armyCenter = _terrainTracker.GetClosestWalkable(_army.GetCenter(), searchRadius: 3);

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

        if (!_detectionTracker.IsStealthEffective()) {
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

        _targetPosition = default;

        _stateMachine.TransitionTo(_sneakAttackStateFactory.CreateInactiveState());
    }

    private bool HasProperTech() {
        return _controller.ResearchedUpgrades.Contains(Upgrades.TunnelingClaws) && _controller.ResearchedUpgrades.Contains(Upgrades.Burrow);
    }

    private static bool IsArmyGettingEngaged(IEnumerable<Unit> army) {
        // TODO GD Track HP loss, EngagedTargetTag is only set on our units
        // TODO GD We will often get hit by the first tank shell / enemy salvo, take that into account
        return false;
    }

    private IEnumerable<Unit> GetGroundEnemiesInSight(IReadOnlyCollection<Unit> army) {
        return _unitsTracker.GetUnits(_unitsTracker.EnemyUnits, Units.Military)
            .Where(enemy => enemy.IsVisible)
            .Where(enemy => !enemy.IsFlying)
            .Where(enemy => army.Any(soldier => enemy.DistanceTo(soldier) <= Math.Max(enemy.UnitTypeData.SightRange, soldier.UnitTypeData.SightRange)));
    }

    private static void BurrowOverlings(IEnumerable<Unit> army) {
        foreach (var soldier in army.Where(soldier => !soldier.IsBurrowed)) {
            soldier.UseAbility(Abilities.BurrowRoachDown);
        }
    }

    private IEnumerable<Unit> GetPriorityTargetsInOperationRadius(IReadOnlyCollection<Unit> army, float operationRadius) {
        return _unitsTracker
            .GetUnits(_unitsTracker.EnemyUnits.Concat(_unitsTracker.EnemyGhostUnits.Values), PriorityTargets)
            .Where(enemy => army.Min(soldier => soldier.DistanceTo(enemy)) <= operationRadius);
    }

    private void DebugTarget() {
        if (_targetPosition == default) {
            return;
        }

        _graphicalDebugger.AddLink(_terrainTracker.WithWorldHeight(_armyCenter), _terrainTracker.WithWorldHeight(_targetPosition), Colors.Magenta);
        _graphicalDebugger.AddSphere(_terrainTracker.WithWorldHeight(_targetPosition), 1, Colors.Magenta);

        if (_isTargetPriority) {
            _graphicalDebugger.AddText("!", size: 20, worldPos: _terrainTracker.WithWorldHeight(_targetPosition).ToPoint());
        }
    }
}
