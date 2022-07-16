using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.GameData;
using Bot.Wrapper;

namespace Bot.Managers;

public class WarManager: IManager {
    private const int GuardDistance = 8;
    private const int SupplyRequiredBeforeAttacking = 18;

    private readonly BattleManager _battleManager;
    private Unit _townHallToDefend;

    private readonly List<BuildOrders.BuildStep> _buildStepRequests = new List<BuildOrders.BuildStep>();
    public IEnumerable<BuildOrders.BuildStep> BuildStepRequests => _buildStepRequests;

    public WarManager() {
        var townHallDefensePosition = GetTownHallDefensePosition(Controller.StartingTownHall, Controller.EnemyLocations[0]);
        _battleManager = new BattleManager(townHallDefensePosition);
        _townHallToDefend = Controller.StartingTownHall;
    }

    public void OnFrame() {
        // Assign forces
        // TODO GD Use queens
        var newSoldiers = Controller.GetUnits(Controller.NewOwnedUnits, Units.ZergMilitary).ToList();
        _battleManager.Assign(newSoldiers);

        // TODO GD Handle multiple managers

        var enemyPosition = Controller.EnemyLocations[0];
        var currentDistanceToEnemy = _townHallToDefend.DistanceTo(enemyPosition);
        var newTownHallToDefend = Controller.GetUnits(Controller.NewOwnedUnits, Units.Hatchery)
            .FirstOrDefault(townHall => townHall.DistanceTo(enemyPosition) < currentDistanceToEnemy); // TODO GD Use pathing instead of direct distance

        // TODO GD Fallback on other townhalls when destroyed
        if (newTownHallToDefend != default) {
            _battleManager.Assign(GetTownHallDefensePosition(newTownHallToDefend, Controller.EnemyLocations[0]));
            _townHallToDefend = newTownHallToDefend;
        }

        if (_battleManager.Army.Sum(soldier => soldier.FoodRequired) >= SupplyRequiredBeforeAttacking && _buildStepRequests.Count == 0) {
            _buildStepRequests.Add(new BuildOrders.BuildStep(BuildType.Train, 0, Units.Roach, 1000));
            _battleManager.Assign(enemyPosition);
        }

        GraphicalDebugger.AddLine(_townHallToDefend.Position, _battleManager.Target, Colors.Red);
        GraphicalDebugger.AddSphere(_battleManager.Target, 1, Colors.Red);
        _battleManager.OnFrame();
    }

    public void ReportUnitDeath(Unit deadUnit) {
        // Nothing to do
    }

    private static Vector3 GetTownHallDefensePosition(Unit townHall, Vector3 threatPosition) {
        // TODO GD Use pathing instead of direct distance
        // TODO GD Use heightMap to set terrain Z
        return townHall.Position.TranslateTowards(threatPosition, GuardDistance, ignoreZAxis: true);
    }
}
