using System.Linq;

namespace Bot.Managers;

public class WarManager: IManager {
    private readonly BattleManager _attackManager = new BattleManager();

    public WarManager() {
        // Nothing to do
    }

    public void OnFrame() {
        // Determine if there are battles going on (Attack, Run by, Defense)
        var attackTarget = Controller.EnemyLocations[0];

        // Assign forces
        // TODO GD Don't send the Queens. At least, not those assigned to a base.
        var newSoldiers = Controller.GetUnits(Controller.NewOwnedUnits, Units.ZergMilitary).ToList();
        _attackManager.Assign(newSoldiers);

        // Dispatch targets
        _attackManager.Assign(attackTarget);

        // Execute managers
        _attackManager.OnFrame();
    }

    public void ReportUnitDeath(Unit deadUnit) {
        // Nothing to do
    }
}
