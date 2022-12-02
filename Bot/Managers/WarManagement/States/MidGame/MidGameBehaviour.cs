using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.GameSense.RegionTracking;
using Bot.Managers.WarManagement.ArmySupervision;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.Managers.WarManagement.States.MidGame;

public class MidGameBehaviour : IWarManagerBehaviour {
    private const float RequiredForceRatioBeforeAttacking = 1.5f;
    private const float MaxSupplyBeforeAttacking = 175;

    private static readonly HashSet<uint> ManageableUnitTypes = Units.ZergMilitary.Except(new HashSet<uint> { Units.Queen, Units.QueenBurrowed }).ToHashSet();

    private BuildRequest _armyBuildRequest = new TargetBuildRequest(BuildType.Train, Units.Roach, targetQuantity: 100, priority: BuildRequestPriority.Low);

    private readonly MidGameBehaviourDebugger _debugger = new MidGameBehaviourDebugger();
    private readonly WarManager _warManager;

    private bool _hasCleanUpStarted = false;

    public Stance Stance { get; private set; } = Stance.Defend;
    public readonly ArmySupervisor AttackSupervisor = new ArmySupervisor();
    public readonly ArmySupervisor DefenseSupervisor = new ArmySupervisor();

    public IAssigner Assigner { get; }
    public IDispatcher Dispatcher { get; }
    public IReleaser Releaser { get; }

    public List<BuildRequest> BuildRequests { get; } = new List<BuildRequest>();

    public MidGameBehaviour(WarManager warManager) {
        _warManager = warManager;

        BuildRequests.Add(_armyBuildRequest);

        Assigner = new WarManagerAssigner<MidGameBehaviour>(this);
        Dispatcher = new MidGameDispatcher(this);
        Releaser = new WarManagerReleaser<MidGameBehaviour>(this);
    }

    public void RecruitmentPhase() {
        if (_hasCleanUpStarted) {
            return;
        }

        _warManager.Assign(Controller.GetUnits(UnitsTracker.NewOwnedUnits, ManageableUnitTypes));
    }

    public void DispatchPhase() {
        // TODO GD We could probably have this logic in a base class
        if (_hasCleanUpStarted) {
            return;
        }

        _warManager.Dispatch(_warManager.ManagedUnits.Where(soldier => soldier.Supervisor == null));
    }

    public void ManagementPhase() {
        if (_hasCleanUpStarted) {
            return;
        }

        BigBrainPlay();
        AdjustBuildRequests();

        AttackSupervisor.OnFrame();
        DefenseSupervisor.OnFrame();

        // TODO GD The war manager could do most of that
        _debugger.OwnForce = _warManager.ManagedUnits.GetForce();
        _debugger.EnemyForce = GetEnemyForce();
        _debugger.CurrentStance = Stance;
        _debugger.BuildPriority = BuildRequests.FirstOrDefault()?.Priority ?? BuildRequestPriority.Low;
        _debugger.BuildBlockCondition = BuildRequests.FirstOrDefault()?.BlockCondition ?? BuildBlockCondition.None;
        _debugger.Debug();
    }

    public bool CleanUp() {
        _hasCleanUpStarted = true;

        if (AttackSupervisor.SupervisedUnits.Any()) {
            AttackSupervisor.Retire();

            // We give one tick so that release orders, like stop or unburrow go through
            return false;
        }

        if (DefenseSupervisor.SupervisedUnits.Any()) {
            DefenseSupervisor.Retire();

            // We give one tick so that release orders, like stop or unburrow go through
            return false;
        }

        return true;
    }

    /// <summary>
    /// Decide between attack and defense and pick good targets based on current map control.
    /// </summary>
    private void BigBrainPlay() {
        // Determine regions to defend
        var regionToDefend = GetRegionToDefend();
        DefenseSupervisor.AssignTarget(regionToDefend.Center, regionToDefend.ApproximatedRadius, canHuntTheEnemy: false);

        // Determine regions to attack
        var regionToAttack = GetRegionToAttack();
        AttackSupervisor.AssignTarget(regionToAttack.Center, regionToDefend.ApproximatedRadius, canHuntTheEnemy: true);

        AttackOrDefend(regionToAttack, regionToDefend);
    }

    /// <summary>
    /// Determines which region to defend next
    /// </summary>
    /// <returns>The region to defend next</returns>
    private static Region GetRegionToDefend() {
        // TODO GD We sometimes try to defend a position that's behind the enemy army while we are losing
        return RegionAnalyzer.Regions.MaxBy(region => RegionTracker.GetDefenseScore(region))!;
    }

    /// <summary>
    /// Determines which region to attack next
    /// </summary>
    /// <returns>The region to attack next</returns>
    private static Region GetRegionToAttack() {
        var valuableEnemyRegions = RegionAnalyzer.Regions
            .Where(region => RegionTracker.GetValue(region, Alliance.Enemy) > UnitEvaluator.Value.Intriguing)
            .ToList();

        if (!valuableEnemyRegions.Any()) {
            valuableEnemyRegions = RegionAnalyzer.Regions;
        }

        // TODO GD Checking only the region's force doesn't take into account that maybe we are forced to go through more enemies to get to our target.
        // TODO GD Maybe consider forces in regions adjacent to the path?
        // TODO GD Also consider the distance to the region to simulate commitment? Or only change target if significant value change
        var regionToAttack = valuableEnemyRegions
            .MaxBy(region => RegionTracker.GetValue(region, Alliance.Enemy) / RegionTracker.GetForce(region, Alliance.Enemy))!;

        return regionToAttack;
    }

    /// <summary>
    /// Determines if we should attack the given region or defend our home.
    /// </summary>
    /// <param name="regionToAttack">The region that we would attack</param>
    /// <param name="regionToDefend">The region that we would defend</param>
    private void AttackOrDefend(Region regionToAttack, Region regionToDefend) {
        if (regionToAttack.IsObstructed) {
            Logger.Error("Trying to attack an obstructed region");
            return;
        }

        if (_warManager.ManagedUnits.Count == 0) {
            // TODO GD Should we do stuff here?
            return;
        }

        var armyRegion = _warManager.ManagedUnits.GetRegion();
        if (armyRegion == null) {
            Logger.Error("AttackOrDefend could not find the region of its army of size {0}", _warManager.ManagedUnits.Count);
            return;
        }

        var pathToTarget = Pathfinder.FindPath(armyRegion, regionToAttack);
        if (pathToTarget == null) {
            Logger.Error("AttackOrDefend could not find a path from the army to its target {0} -> {1}", armyRegion, regionToAttack);
            return;
        }

        var ourForce = _warManager.ManagedUnits.GetForce();

        // TODO GD Maybe consider units near the path as well?
        var enemyForce = pathToTarget.Sum(region => RegionTracker.GetForce(region, Alliance.Enemy));

        if (ourForce > enemyForce * RequiredForceRatioBeforeAttacking || Controller.CurrentSupply >= MaxSupplyBeforeAttacking) {
            if (Stance == Stance.Defend) {
                DefenseSupervisor.Retire();
            }

            Stance = Stance.Attack;
            _debugger.Target = regionToAttack;
        }
        else {
            if (Stance == Stance.Attack) {
                AttackSupervisor.Retire();
            }

            Stance = Stance.Defend;
            _debugger.Target = regionToDefend;
        }
    }

    /// <summary>
    /// Request more or less army based on what the WarManager has and what the enemy has and is doing.
    /// </summary>
    private void AdjustBuildRequests() {
        var unitTypeToProduce = GetUnitTypeToProduce();
        if (_armyBuildRequest.UnitOrUpgradeType != unitTypeToProduce) {
            BuildRequests.Remove(_armyBuildRequest);

            _armyBuildRequest = new TargetBuildRequest(BuildType.Train, unitTypeToProduce, targetQuantity: 100, priority: BuildRequestPriority.Low);
            BuildRequests.Add(_armyBuildRequest);
        }

        // TODO GD Consider units in production too
        var ourForce = _warManager.ManagedUnits.GetForce();
        // TODO GD Exclude buildings?
        var enemyForce = GetEnemyForce();

        if (ourForce * 1.5 < enemyForce) {
            _armyBuildRequest.BlockCondition = BuildBlockCondition.MissingMinerals | BuildBlockCondition.MissingProducer | BuildBlockCondition.MissingTech;
        }
        else {
            _armyBuildRequest.BlockCondition = BuildBlockCondition.None;
        }

        // TODO GD Try to see if they are attacking or not. If they're not, keep droning
        if (ourForce < enemyForce) {
            _armyBuildRequest.Priority = BuildRequestPriority.Medium;
        }
        else {
            _armyBuildRequest.Priority = BuildRequestPriority.Low;
        }
    }

    /// <summary>
    /// Determines the unit type to produce given the current situation.
    /// Right now, it just checks if it can make roaches, but in the future it might check against the enemy composition.
    /// </summary>
    /// <returns>The unit type id to produce.</returns>
    private static uint GetUnitTypeToProduce() {
        if (Controller.IsUnlocked(Units.Roach, TechTree.UnitPrerequisites)) {
            return Units.Roach;
        }

        if (Controller.GetUnits(UnitsTracker.OwnedUnits, Units.SpawningPool).Any()) {
            return Units.Zergling;
        }

        // TODO GD Not sure if this is good
        return Units.Drone;
    }

    /// <summary>
    /// Returns the enemy force
    /// </summary>
    /// <returns></returns>
    private static float GetEnemyForce() {
        return UnitsTracker.EnemyMemorizedUnits.Values.Concat(UnitsTracker.EnemyUnits).GetForce();
    }
}
