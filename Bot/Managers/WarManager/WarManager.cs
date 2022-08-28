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
    private bool _rushInProgress;

    public IEnumerable<BuildFulfillment> BuildFulfillments => _buildRequests.Select(buildRequest => buildRequest.Fulfillment);
    public IEnumerable<Unit> ManagedUnits => _soldiers;

    public WarManager() {
        var townHallDefensePosition = GetTownHallDefensePosition(MapAnalyzer.StartingLocation, MapAnalyzer.EnemyStartingLocation);
        _armyManager = new ArmyManager();
        _armyManager.Assign(townHallDefensePosition, GuardRadius, false);
        _townHallToDefend = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery).First(townHall => townHall.Position == MapAnalyzer.StartingLocation);
    }

    // TODO GD Use multiple managers, probably
    public void OnFrame() {
        DispatchSoldiers(Controller.GetUnits(UnitsTracker.NewOwnedUnits, ManageableUnitTypes).ToList());
        ScanForEndangeredExpands();

        // TODO Probably use states
        if (!HandleRushes()) {
            var enemyPosition = MapAnalyzer.EnemyStartingLocation;

            if (!_hasAssaultStarted) {
                DefendNewTownHalls(enemyPosition);
            }

            if (!_hasAssaultStarted && _armyManager.Army.GetForce() >= ForceRequiredBeforeAttacking) {
                StartTheAssault(enemyPosition);
            }
        }

        _armyManager.OnFrame();
    }

    public void Release(Unit unit) {
        if (_soldiers.Remove(unit)) {
            unit.Stop(); // TODO GD Automate this on managers and supervisors

            unit.Manager = null; // TODO GD Automate this on managers and supervisors
            unit.RemoveDeathWatcher(this); // TODO GD Automate this on managers and supervisors

            _armyManager.Release(unit);
        }
    }

    private void DispatchSoldiers(List<Unit> soldiers) {
        soldiers = soldiers.Where(soldier => !_soldiers.Contains(soldier)).ToList();

        foreach (var soldier in soldiers) {
            soldier.Stop();

            soldier.Manager = this;
            soldier.AddDeathWatcher(this);
            _soldiers.Add(soldier);

            ChangelingTargetingModule.Install(soldier);
        }

        _armyManager.Assign(soldiers);
    }

    private void ScanForEndangeredExpands() {
        // TODO GD Don't run this all the time
        var expandsInDanger = DangerScanner.GetEndangeredExpands().ToHashSet();
        foreach (var expandNewlyInDanger in expandsInDanger.Except(_expandsInDanger)) {
            Logger.Info("(WarManager) An expand is newly in danger: {0}", expandNewlyInDanger);
        }

        foreach (var expandNoLongerInDanger in _expandsInDanger.Except(expandsInDanger)) {
            Logger.Info("(WarManager) An expand is no longer in danger: {0}", expandNoLongerInDanger);
        }

        _expandsInDanger = expandsInDanger;
    }

    private bool HandleRushes() {
        if (_expandsInDanger.Count > 0 && Controller.Frame <= Controller.SecsToFrames(RushTimingInSeconds)) {
            Controller.SetRealTime();

            if (!_rushTagged) {
                Controller.TagGame($"EarlyRush_{Controller.GetGameTimeString()}");
                _rushTagged = true;
            }

            if (!_rushInProgress) {
                _rushInProgress = true;
                // TODO GD We should know which expand to defend and be able to switch
                _armyManager.Assign(GetTownHallDefensePosition(_expandsInDanger.First().Position, MapAnalyzer.EnemyStartingLocation), GuardRadius, false);
            }

            // TODO GD We should be smarter about how many units we draft
            var supervisedTownHalls = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.TownHalls).Where(unit => unit.Supervisor != null);
            foreach (var expandToDefend in supervisedTownHalls) {
                var draftedUnits = expandToDefend.Supervisor.ManagedUnits.Where(unit => Units.Workers.Contains(unit.UnitType) || unit.UnitType == Units.Queen).ToList();
                DispatchSoldiers(draftedUnits);
            }
        }
        else if (_rushInProgress) {
            var unitsToReturn = _soldiers.Where(soldier => Units.Workers.Contains(soldier.UnitType) || soldier.UnitType == Units.Queen);
            foreach (var unitToReturn in unitsToReturn) {
                Release(unitToReturn);
            }

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
