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

    private readonly ArmySupervisor _armySupervisor;
    private Unit _townHallToDefend;

    private readonly List<BuildRequest> _buildRequests = new List<BuildRequest>();

    public override IEnumerable<BuildFulfillment> BuildFulfillments => _buildRequests.Select(buildRequest => buildRequest.Fulfillment);

    public static WarManager Create() {
        var manager = new WarManager();
        manager.Init();

        return manager;
    }

    private WarManager() {
        _armySupervisor = ArmySupervisor.Create();

        _townHallToDefend = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery).First(townHall => townHall.Position == MapAnalyzer.StartingLocation);

        var townHallDefensePosition = GetTownHallDefensePosition(_townHallToDefend.Position, MapAnalyzer.EnemyStartingLocation);
        _armySupervisor.AssignTarget(townHallDefensePosition, GuardRadius, false);
    }

    protected override IAssigner CreateAssigner() {
        return new WarManagerAssigner(this);
    }

    protected override IDispatcher CreateDispatcher() {
        return new WarManagerDispatcher(this);
    }

    protected override IReleaser CreateReleaser() {
        return new WarManagerReleaser(this);
    }

    protected override void AssignUnits() {
        Assign(Controller.GetUnits(UnitsTracker.NewOwnedUnits, ManageableUnitTypes));
    }

    protected override void DispatchUnits() {
        Dispatch(_soldiers.Where(soldier => soldier.Supervisor == null));
    }

    // TODO GD Use multiple supervisors, probably
    protected override void Manage() {
        ScanForEndangeredExpands();

        // TODO Use states
        if (!HandleRushes()) {
            var enemyPosition = MapAnalyzer.EnemyStartingLocation;

            if (!_hasAssaultStarted) {
                DefendNewTownHalls(enemyPosition);
            }

            if (!_hasAssaultStarted && _armySupervisor.Army.GetForce() >= ForceRequiredBeforeAttacking) {
                StartTheAssault(enemyPosition);
            }
        }

        _armySupervisor.OnFrame();
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
        if (_expandsInDanger.Count > 0 && Controller.Frame <= Controller.SecsToFrames(RushTimingInSeconds)) {
            if (!_rushTagged) {
                Controller.TagGame($"EarlyRush_{Controller.GetGameTimeString()}"); // TODO GD Make utility functions are tags so we know which tags exist
                _rushTagged = true;
            }

            if (!_rushInProgress) {
                _rushInProgress = true;
                // TODO GD We should know which expand to defend and be able to switch
                _armySupervisor.AssignTarget(GetTownHallDefensePosition(_expandsInDanger.First().Position, MapAnalyzer.EnemyStartingLocation), GuardRadius, false);
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

    private void DefendNewTownHalls(Vector3 enemyPosition) {
        var pathToTheEnemy = Pathfinder.FindPath(_townHallToDefend.Position, enemyPosition);
        if (pathToTheEnemy == null) {
            Logger.Error("<DefendNewTownHalls> No path found from base {0} to enemy base {1}", _townHallToDefend.Position, enemyPosition);
            return;
        }

        var currentDistanceToEnemy = pathToTheEnemy.Count; // Not exact, but the distance difference should not matter
        var newTownHallToDefend = Controller.GetUnits(UnitsTracker.NewOwnedUnits, Units.Hatchery)
            .FirstOrDefault(townHall => Pathfinder.FindPath(townHall.Position, enemyPosition).Count < currentDistanceToEnemy);

        // TODO GD Fallback on other townhalls when destroyed
        if (newTownHallToDefend != default) {
            _armySupervisor.AssignTarget(GetTownHallDefensePosition(newTownHallToDefend.Position, MapAnalyzer.EnemyStartingLocation), GuardRadius, false);
            _townHallToDefend = newTownHallToDefend;
        }
    }

    private void StartTheAssault(Vector3 enemyPosition) {
        _hasAssaultStarted = true;
        _armySupervisor.AssignTarget(enemyPosition, AttackRadius);

        // TODO GD Handle this better
        if (_buildRequests.Count == 0) {
            _buildRequests.Add(new TargetBuildRequest(BuildType.Train, Units.Roach, targetQuantity: 100));
        }
    }

    private static Vector3 GetTownHallDefensePosition(Vector3 townHallPosition, Vector3 threatPosition) {
        var pathToThreat = Pathfinder.FindPath(townHallPosition, threatPosition);
        var guardDistance = Math.Min(pathToThreat.Count, GuardDistance);

        return pathToThreat[guardDistance];
    }

    public override string ToString() {
        return "WarManager";
    }
}
