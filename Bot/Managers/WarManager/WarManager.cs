using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Builds;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.Managers.ArmySupervision;
using Bot.MapKnowledge;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.Managers;

public partial class WarManager: Manager {
    private const int GuardDistance = 6;
    private const int GuardRadius = 8;
    private const int AttackRadius = 999; // Basically the whole map
    private const int ForceRequiredBeforeAttacking = 18;
    private const int RushTimingInSeconds = (int)(5 * 60);

    // TODO GD Use queens?
    private static readonly HashSet<uint> ManageableUnitTypes = Units.ZergMilitary.Except(new HashSet<uint> { Units.Queen, Units.QueenBurrowed }).ToHashSet();

    private bool _rushTagged = false;
    private bool _rushInProgress;
    private HashSet<Unit> _expandsInDanger = new HashSet<Unit>();

    private bool _hasAssaultStarted = false;
    private readonly HashSet<Unit> _soldiers = new HashSet<Unit>();

    private readonly ArmySupervisor _groundArmySupervisor = new ArmySupervisor();
    private readonly ArmySupervisor _airArmySupervisor = new ArmySupervisor();
    private Unit _townHallToDefend;

    private readonly List<BuildRequest> _buildRequests = new List<BuildRequest>();
    private bool _terranFinisherInitiated = false;

    public override IEnumerable<BuildFulfillment> BuildFulfillments => _buildRequests.Select(buildRequest => buildRequest.Fulfillment);

    protected override IAssigner Assigner { get; }
    protected override IDispatcher Dispatcher { get; }
    protected override IReleaser Releaser { get; }

    public WarManager() {
        Assigner = new WarManagerAssigner(this);
        Dispatcher = new WarManagerDispatcher(this);
        Releaser = new WarManagerReleaser(this);

        _townHallToDefend = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery).First(townHall => townHall.Position.ToVector2() == MapAnalyzer.StartingLocation);

        var townHallDefensePosition = GetTownHallDefensePosition(_townHallToDefend.Position.ToVector2(), MapAnalyzer.EnemyStartingLocation);
        _groundArmySupervisor.AssignTarget(townHallDefensePosition, GuardRadius, false);
        _airArmySupervisor.AssignTarget(townHallDefensePosition, AttackRadius);
    }

    protected override void AssignUnits() {
        Assign(Controller.GetUnits(UnitsTracker.NewOwnedUnits, ManageableUnitTypes));
    }

    protected override void DispatchUnits() {
        Dispatch(_soldiers.Where(soldier => soldier.Supervisor == null));
    }

    protected override void Manage() {
        ScanForEndangeredExpands();

        // TODO Use states
        if (!HandleRushes()) {
            var enemyPosition = MapAnalyzer.EnemyStartingLocation;

            if (!_hasAssaultStarted) {
                DefendNewTownHalls(enemyPosition);
            }

            if (!_hasAssaultStarted && _groundArmySupervisor.Army.GetForce() >= ForceRequiredBeforeAttacking) {
                StartTheAssault(enemyPosition);
            }

            if (_hasAssaultStarted && ShouldFinishOffTerran()) {
                FinishOffTerran();
            }
        }

        // TODO GD Send this task to the supervisor instead
        if (_terranFinisherInitiated && Controller.AvailableSupply < 2) {
            foreach (var supervisedUnit in _groundArmySupervisor.SupervisedUnits.Where(unit => unit.IsBurrowed)) {
                supervisedUnit.UseAbility(Abilities.BurrowRoachUp);
            }

            var unburrowedUnits = _groundArmySupervisor.SupervisedUnits.Where(unit => !unit.IsBurrowed).ToList();
            if (unburrowedUnits.Count > 0) {
                var unitToSacrifice = unburrowedUnits[0];
                foreach (var unburrowedUnit in unburrowedUnits) {
                    unburrowedUnit.Attack(unitToSacrifice);
                }
            }
        }
        else {
            _groundArmySupervisor.OnFrame();
        }

        _airArmySupervisor.OnFrame();
    }

    private void ScanForEndangeredExpands() {
        // TODO GD Don't run this all the time
        var expandsInDanger = DangerScanner.GetEndangeredExpands().ToHashSet();
        foreach (var expandNewlyInDanger in expandsInDanger.Except(_expandsInDanger)) {
            Logger.Info("({0}) An expand is newly in danger: {1}", this, expandNewlyInDanger);
        }

        foreach (var expandNoLongerInDanger in _expandsInDanger.Except(expandsInDanger)) {
            Logger.Info("({0}) An expand is no longer in danger: {1}", this, expandNoLongerInDanger);
        }

        _expandsInDanger = expandsInDanger;
    }

    private bool HandleRushes() {
        if (_expandsInDanger.Count > 0 && Controller.Frame <= TimeUtils.SecsToFrames(RushTimingInSeconds)) {
            if (!_rushTagged) {
                Controller.TagGame($"EarlyRush_{TimeUtils.GetGameTimeString()}"); // TODO GD Make utility functions for tags so we know which tags exist
                _rushTagged = true;
            }

            if (!_rushInProgress) {
                _rushInProgress = true;
                // TODO GD We should know which expand to defend and be able to switch
                var townHallDefensePosition = GetTownHallDefensePosition(_expandsInDanger.First().Position.ToVector2(), MapAnalyzer.EnemyStartingLocation);
                _groundArmySupervisor.AssignTarget(townHallDefensePosition, GuardRadius, false);
            }

            // TODO GD We should be smarter about how many units we draft
            var supervisedTownHalls = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.TownHalls).Where(unit => unit.Supervisor != null);
            foreach (var expandToDefend in supervisedTownHalls) {
                var draftedUnits = expandToDefend.Supervisor.SupervisedUnits.Where(unit => Units.Workers.Contains(unit.UnitType) || unit.UnitType == Units.Queen);
                Assign(draftedUnits);
            }
        }
        else if (_rushInProgress && _expandsInDanger.Count <= 0) {
            var unitsToReturn = _soldiers.Where(soldier => Units.Workers.Contains(soldier.UnitType) || soldier.UnitType == Units.Queen);
            Release(unitsToReturn);

            _rushInProgress = false;
        }

        return _rushInProgress;
    }

    private void DefendNewTownHalls(Vector2 enemyPosition) {
        var pathToTheEnemy = Pathfinder.FindPath(_townHallToDefend.Position.ToVector2(), enemyPosition);
        if (pathToTheEnemy == null) {
            Logger.Error("<DefendNewTownHalls> No path found from base {0} to enemy base {1}", _townHallToDefend.Position, enemyPosition);
            return;
        }

        var currentDistanceToEnemy = pathToTheEnemy.Count; // Not exact, but the distance difference should not matter
        var newTownHallToDefend = Controller.GetUnits(UnitsTracker.NewOwnedUnits, Units.Hatchery)
            .FirstOrDefault(townHall => Pathfinder.FindPath(townHall.Position.ToVector2(), enemyPosition).Count < currentDistanceToEnemy);

        // TODO GD Fallback on other townhalls when destroyed
        if (newTownHallToDefend != default) {
            _groundArmySupervisor.AssignTarget(GetTownHallDefensePosition(newTownHallToDefend.Position.ToVector2(), MapAnalyzer.EnemyStartingLocation), GuardRadius, false);
            _townHallToDefend = newTownHallToDefend;
        }
    }

    private void StartTheAssault(Vector2 enemyPosition) {
        _hasAssaultStarted = true;
        _groundArmySupervisor.AssignTarget(enemyPosition, AttackRadius);

        // TODO GD Handle this better
        if (_buildRequests.Count == 0) {
            _buildRequests.Add(new TargetBuildRequest(BuildType.Train, Units.Roach, targetQuantity: 100));
        }
    }

    private static Vector2 GetTownHallDefensePosition(Vector2 townHallPosition, Vector2 threatPosition) {
        var pathToThreat = Pathfinder.FindPath(townHallPosition, threatPosition);
        var guardDistance = Math.Min(pathToThreat.Count, GuardDistance);

        return pathToThreat[guardDistance];
    }

    // TODO GD Probably need a class for this
    /// <summary>
    /// Some Terran will fly their buildings.
    /// Check if they are basically dead and we should start dealing with the flying buildings.
    /// </summary>
    /// <returns>True if we should start handling flying terran buildings</returns>
    private static bool ShouldFinishOffTerran() {
        if (Controller.EnemyRace != Race.Terran) {
            return false;
        }

        if (Controller.Frame < TimeUtils.SecsToFrames(12 * 60)) {
            return false;
        }

        if (Controller.Frame % TimeUtils.SecsToFrames(60) != 0) {
            return false;
        }

        if (MapAnalyzer.ExplorationRatio < 0.80 || !ExpandAnalyzer.ExpandLocations.All(expandLocation => VisibilityTracker.IsExplored(expandLocation.Position))) {
            return false;
        }

        return Controller.GetUnits(UnitsTracker.EnemyUnits, Units.Buildings).All(building => building.IsFlying);
    }

    /// <summary>
    /// Create anti-air units to deal with terran flying buildings.
    /// </summary>
    private void FinishOffTerran() {
        if (_terranFinisherInitiated) {
            return;
        }

        _buildRequests.Clear();
        _buildRequests.Add(new TargetBuildRequest(BuildType.Build, Units.Spire, targetQuantity: 1));
        _buildRequests.Add(new TargetBuildRequest(BuildType.Train, Units.Corruptor, targetQuantity: 10));
        _terranFinisherInitiated = true;
    }

    public override string ToString() {
        return "WarManager";
    }
}
