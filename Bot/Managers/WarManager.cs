using System.Linq;
using System.Numerics;
using Bot.Wrapper;

namespace Bot.Managers;

public class WarManager: IManager {
    private const int GuardDistance = 8;
    private const int SupplyRequiredBeforeAttacking = 18;

    private readonly BattleManager _battleManager;
    private Unit _townHallToDefend;

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

        if (_battleManager.Army.Sum(soldier => soldier.Supply) >= SupplyRequiredBeforeAttacking) {
            _battleManager.Assign(enemyPosition);
        }

        Debugger.AddLine(_townHallToDefend.Position, _battleManager.Target, Colors.Red);
        Debugger.AddSphere(_battleManager.Target, 1, Colors.Red);
        _battleManager.OnFrame();
    }

    public void ReportUnitDeath(Unit deadUnit) {
        // Nothing to do
    }

    private static Vector3 GetTownHallDefensePosition(Unit townHall, Vector3 threatPosition) {
        // TODO GD Use pathing instead of direct distance
        var townHallPosition = townHall.Position;
        var threatDirection = threatPosition - townHallPosition;
        threatDirection.Z = 0; // TODO GD Use heightMap to set terrain Z

        var townHallDefenseDirection = Vector3.Normalize(threatDirection) * GuardDistance;

        return townHallPosition + townHallDefenseDirection;
    }
}
