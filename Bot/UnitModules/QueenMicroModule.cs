using System.Linq;

namespace Bot.UnitModules;

public class QueenMicroModule: IUnitModule {
    public const string Tag = "queen-micro-module";

    private readonly Unit _queen;
    private readonly Unit _assignedTownHall;

    public static void Install(Unit queen, Unit assignedTownHall) {
        queen.Modules.Add(Tag, new QueenMicroModule(queen, assignedTownHall));
    }

    public static QueenMicroModule Uninstall(Unit worker) {
        var module = GetFrom(worker);
        worker.Modules.Remove(Tag);

        return module;
    }

    public static QueenMicroModule GetFrom(Unit worker) {
        if (worker.Modules.TryGetValue(Tag, out var module)) {
            return module as QueenMicroModule;
        }

        return null;
    }

    private QueenMicroModule(Unit queen, Unit assignedTownHall) {
        _queen = queen;
        _assignedTownHall = assignedTownHall;
    }

    public void Execute() {
        // TODO GD Find the energy cost
        if (_queen.RawUnitData.Energy >= 25 && _queen.Orders.All(order => order.AbilityId != Abilities.InjectLarvae)) {
            _queen.UseAbility(Abilities.InjectLarvae, targetUnitTag: _assignedTownHall.Tag);
        }

        // TODO GD Spawn some creep with energy overflow
    }
}
