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
    private const int GuardDistance = 8;
    private const int GuardRadius = 8;
    private const int AttackRadius = 999; // Basically the whole map
    private const int ForceRequiredBeforeAttacking = 18;

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

    // TODO GD Use queens?
    // TODO GD Use multiple managers, probably
    public void OnFrame() {
        var newSoldiers = Controller.GetUnits(UnitsTracker.NewOwnedUnits, Units.ZergMilitary).ToList();

        foreach (var soldier in newSoldiers) {
            soldier.Manager = this;
            soldier.AddDeathWatcher(this);
            _soldiers.Add(soldier);

            ChangelingTargetingModule.Install(soldier);
        }

        _armyManager.Assign(newSoldiers);

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
        _armyManager.Release(unit);
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
