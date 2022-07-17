using Bot.GameData;

namespace Bot.UnitModules;

public class QueenMicroModule: IUnitModule, IWatchUnitsDie {
    public const string Tag = "queen-micro-module";

    private Unit _queen;
    private Unit _assignedTownHall;

    public static void Install(Unit queen, Unit assignedTownHall) {
        if (assignedTownHall == null) {
            Logger.Error("Trying to install a QueenMicroModule with a null townHall");
            return;
        }

        if (queen == null) {
            Logger.Error("Trying to install a QueenMicroModule with a null queen");
            return;
        }

        queen.Modules.Add(Tag, new QueenMicroModule(queen, assignedTownHall));
    }

    private QueenMicroModule(Unit queen, Unit assignedTownHall) {
        _queen = queen;
        _queen.AddDeathWatcher(this);

        _assignedTownHall = assignedTownHall;
        _assignedTownHall.AddDeathWatcher(this);
    }

    public void Execute() {
        if (_queen == null || _assignedTownHall == null) {
            return;
        }

        if (_queen.HasEnoughEnergy(Abilities.InjectLarvae)) {
            _queen.UseAbility(Abilities.InjectLarvae, targetUnitTag: _assignedTownHall.Tag);
        }

        // TODO GD Spawn some creep with energy overflow
    }

    public void ReportUnitDeath(Unit deadUnit) {
        if (deadUnit == _assignedTownHall) {
            _assignedTownHall = null;
        }
        else if (deadUnit == _queen) {
            _queen = null;
        }
    }
}
