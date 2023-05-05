using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.Debugging;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.Managers.WarManagement.ArmySupervision;
using Bot.Tagging;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.Managers.WarManagement.States.Finisher;

/// <summary>
/// The finisher behavior will hunt down any remaining building on the map and explore areas that are not visible.
/// It will also produce corruptors to hunt flying terran buildings.
/// </summary>
public class FinisherBehaviour : IWarManagerBehaviour {
    private static readonly HashSet<uint> ManageableUnitTypes = Units.ZergMilitary.Except(new HashSet<uint> { Units.Queen, Units.QueenBurrowed }).ToHashSet();

    private readonly ITaggingService _taggingService;
    private readonly IEnemyRaceTracker _enemyRaceTracker;
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly IBuildRequestFactory _buildRequestFactory;

    private readonly FinisherBehaviourDebugger _debugger;
    private readonly WarManager _warManager;

    private bool _isTerranFinisherInitiated = false;

    public IAssigner Assigner { get; }
    public IDispatcher Dispatcher { get; }
    public IReleaser Releaser { get; }

    private readonly BuildRequest _corruptorsBuildRequest;
    private readonly BuildRequest _armyBuildRequest;
    public List<BuildRequest> BuildRequests { get; } = new List<BuildRequest>();

    public readonly ArmySupervisor AttackSupervisor;
    public readonly ArmySupervisor TerranFinisherSupervisor;

    public FinisherBehaviour(
        WarManager warManager,
        ITaggingService taggingService,
        IEnemyRaceTracker enemyRaceTracker,
        IVisibilityTracker visibilityTracker,
        IDebuggingFlagsTracker debuggingFlagsTracker,
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IRegionsTracker regionsTracker,
        IWarSupervisorFactory warSupervisorFactory,
        IBuildRequestFactory buildRequestFactory
    ) {
        _warManager = warManager;
        _taggingService = taggingService;
        _enemyRaceTracker = enemyRaceTracker;
        _visibilityTracker = visibilityTracker;
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _regionsTracker = regionsTracker;
        _buildRequestFactory = buildRequestFactory;

        _debugger = new FinisherBehaviourDebugger(debuggingFlagsTracker);
        AttackSupervisor = warSupervisorFactory.CreateArmySupervisor();
        TerranFinisherSupervisor = warSupervisorFactory.CreateArmySupervisor();

        _corruptorsBuildRequest = _buildRequestFactory.CreateTargetBuildRequest(BuildType.Train, Units.Corruptor, targetQuantity: 0, priority: BuildRequestPriority.VeryHigh, blockCondition: BuildBlockCondition.All);
        _armyBuildRequest = _buildRequestFactory.CreateTargetBuildRequest(BuildType.Train, Units.Roach, targetQuantity: 100, priority: BuildRequestPriority.Normal);
        BuildRequests.Add(_armyBuildRequest);

        Assigner = new WarManagerAssigner<FinisherBehaviour>(this, _unitsTracker);
        Dispatcher = new FinisherDispatcher(this);
        Releaser = new WarManagerReleaser<FinisherBehaviour>(this);

        var target = _terrainTracker.GetClosestWalkable(_warManager.ManagedUnits.GetCenter(), searchRadius: 3);
        AttackSupervisor.AssignTarget(target, 999, canHuntTheEnemy: true);
        TerranFinisherSupervisor.AssignTarget(target, 999, canHuntTheEnemy: true);
    }

    public void RecruitmentPhase() {
        _warManager.Assign(Controller.GetUnits(_unitsTracker.NewOwnedUnits, ManageableUnitTypes));
    }

    public void DispatchPhase() {
        _warManager.Dispatch(_warManager.ManagedUnits.Where(soldier => soldier.Supervisor == null));
    }

    public void ManagementPhase() {
        if (ShouldFinishOffTerran(_enemyRaceTracker.EnemyRace)) {
            FinishOffTerran();
        }

        if (ShouldFreeSomeSupply()) {
            FreeSomeSupply();
        }
        else {
            AttackSupervisor.OnFrame();
        }

        TerranFinisherSupervisor.OnFrame();

        _debugger.OwnForce = _warManager.ManagedUnits.GetForce();
        _debugger.EnemyForce = GetEnemyForce();
        _debugger.Debug();
    }

    public bool CleanUp() {
        throw new System.NotImplementedException("The finisher behaviour is a final state, cleanup should not be called");
    }

    /// <summary>
    /// Some Terran will fly their buildings.
    /// Check if they are basically dead and we should start dealing with the flying buildings.
    /// </summary>
    /// <returns>True if we should start handling flying terran buildings</returns>
    private bool ShouldFinishOffTerran(Race enemyRace) {
        if (enemyRace != Race.Terran) {
            return false;
        }

        if (Controller.Frame < TimeUtils.SecsToFrames(10 * 60)) {
            return false;
        }

        if (Controller.Frame % TimeUtils.SecsToFrames(5) != 0) {
            return false;
        }

        if (_terrainTracker.ExplorationRatio < 0.80 || !_regionsTracker.ExpandLocations.All(expandLocation => _visibilityTracker.IsExplored(expandLocation.Position))) {
            return false;
        }

        return Controller.GetUnits(_unitsTracker.EnemyUnits, Units.Buildings).All(building => building.IsFlying);
    }

    /// <summary>
    /// Create anti-air units to deal with terran flying buildings.
    /// </summary>
    private void FinishOffTerran() {
        if (_isTerranFinisherInitiated) {
            return;
        }

        BuildRequests.Add(_buildRequestFactory.CreateTargetBuildRequest(BuildType.Build, Units.Spire, targetQuantity: 1, priority: BuildRequestPriority.VeryHigh, blockCondition: BuildBlockCondition.All));

        _corruptorsBuildRequest.Requested = 10;
        BuildRequests.Add(_corruptorsBuildRequest);

        _isTerranFinisherInitiated = true;
        _taggingService.TagTerranFinisher();
    }

    /// <summary>
    /// Determines if supply should be freed to let the TerranFinisher do its job
    /// </summary>
    /// <returns></returns>
    private bool ShouldFreeSomeSupply() {
        return Controller.AvailableSupply < 2 && _corruptorsBuildRequest.Fulfillment.Remaining > 0;
    }

    /// <summary>
    /// Orders the army to kill 1 of their own to free some supply
    /// </summary>
    private void FreeSomeSupply() {
        foreach (var supervisedUnit in AttackSupervisor.SupervisedUnits.Where(unit => unit.IsBurrowed)) {
            supervisedUnit.UseAbility(Abilities.BurrowRoachUp);
        }

        var unburrowedUnits = AttackSupervisor.SupervisedUnits.Where(unit => !unit.IsBurrowed).ToList();
        if (unburrowedUnits.Count > 0) {
            var unitToSacrifice = unburrowedUnits[0];
            foreach (var unburrowedUnit in unburrowedUnits) {
                unburrowedUnit.Attack(unitToSacrifice);
            }
        }
    }

    /// <summary>
    /// Returns the enemy force
    /// </summary>
    /// <returns></returns>
    private float GetEnemyForce() {
        return _unitsTracker.EnemyMemorizedUnits.Values.Concat(_unitsTracker.EnemyUnits).GetForce();
    }
}
