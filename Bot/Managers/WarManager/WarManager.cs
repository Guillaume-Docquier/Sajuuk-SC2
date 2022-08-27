using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Builds;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.Managers.ArmyManagement;
using Bot.MapKnowledge;
using Bot.UnitModules;

namespace Bot.Managers;

public class WarManager: IManager {
    private const int GuardDistance = 6;
    private const int GuardRadius = 8;
    private const int AttackRadius = 999; // Basically the whole map
    private const int ForceRequiredBeforeAttacking = 18;
    private const int RushTimingInSeconds = (int)(4.5 * 60);

    // TODO GD Use queens?
    private static readonly HashSet<uint> ManageableUnitTypes = Units.ZergMilitary.Except(new HashSet<uint> { Units.Queen, Units.QueenBurrowed }).ToHashSet();

    private bool _rushTagged = false;
    private HashSet<Unit> _expandsInDanger = new HashSet<Unit>();

    private bool _hasAssaultStarted = false;
    private readonly HashSet<Unit> _soldiers = new HashSet<Unit>();

    private readonly ArmyManager _armyManager;
    private Unit _townHallToDefend;

    private readonly List<BuildRequest> _buildRequests = new List<BuildRequest>();

    public IEnumerable<BuildFulfillment> BuildFulfillments => _buildRequests.Select(buildRequest => buildRequest.Fulfillment);

    public WarManager() {
        var townHallDefensePosition = GetTownHallDefensePosition(MapAnalyzer.StartingLocation, MapAnalyzer.EnemyStartingLocation);
        _armyManager = new ArmyManager();
        _armyManager.Assign(townHallDefensePosition, GuardRadius, false);
        _townHallToDefend = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery).First(townHall => townHall.Position == MapAnalyzer.StartingLocation);
    }

    // TODO GD Use multiple managers, probably
    public void OnFrame() {
        var newSoldiers = Controller.GetUnits(UnitsTracker.NewOwnedUnits, ManageableUnitTypes).ToList();

        foreach (var soldier in newSoldiers) {
            soldier.Manager = this;
            soldier.AddDeathWatcher(this);
            _soldiers.Add(soldier);

            ChangelingTargetingModule.Install(soldier);
        }

        _armyManager.Assign(newSoldiers);

        // TODO GD Don't run this all the time
        var expandsInDanger = DangerScanner.GetEndangeredExpands().ToHashSet();
        foreach (var expandNewlyInDanger in expandsInDanger.Except(_expandsInDanger)) {
            Logger.Info("(WarManager) An expand is newly in danger: {0}", expandNewlyInDanger);
        }

        foreach (var expandNoLongerInDanger in _expandsInDanger.Except(expandsInDanger)) {
            Logger.Info("(WarManager) An expand is no longer in danger: {0}", expandNoLongerInDanger);
        }

        _expandsInDanger = expandsInDanger;

        if (!_rushTagged && expandsInDanger.Count > 0 && Controller.Frame <= Controller.SecsToFrames(RushTimingInSeconds)) {
            Controller.TagGame($"EarlyRush_{Controller.GetGameTimeString()}");
            _rushTagged = true;
        }

        var enemyPosition = MapAnalyzer.EnemyStartingLocation;

        if (!_hasAssaultStarted) {
            DefendNewTownHalls(enemyPosition);
        }

        if (!_hasAssaultStarted && _armyManager.Army.GetForce() >= ForceRequiredBeforeAttacking) {
            StartTheAssault(enemyPosition);
        }

        _armyManager.OnFrame();
    }

    public void Release(Unit unit) {
        if (_soldiers.Remove(unit)) {
            _armyManager.Release(unit);
        }
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
            _armyManager.Assign(GetTownHallDefensePosition(newTownHallToDefend.Position, MapAnalyzer.EnemyStartingLocation), GuardRadius, false);
            _townHallToDefend = newTownHallToDefend;
        }
    }

    private void StartTheAssault(Vector3 enemyPosition) {
        _hasAssaultStarted = true;
        _armyManager.Assign(enemyPosition, AttackRadius);

        // TODO GD Handle this better
        if (_buildRequests.Count == 0) {
            _buildRequests.Add(new TargetBuildRequest(BuildType.Train, Units.Roach, targetQuantity: 100));
        }
    }

    public void Retire() {
        throw new NotImplementedException();
    }

    public void ReportUnitDeath(Unit deadUnit) {
        _soldiers.Remove(deadUnit);
    }

    private static Vector3 GetTownHallDefensePosition(Vector3 townHallPosition, Vector3 threatPosition) {
        var pathToThreat = Pathfinder.FindPath(townHallPosition, threatPosition);
        var guardDistance = Math.Min(pathToThreat.Count, GuardDistance);

        return pathToThreat[guardDistance];
    }
}
