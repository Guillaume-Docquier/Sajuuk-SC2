using Bot.GameData;
using Bot.Wrapper;

namespace Bot.UnitModules;

public class BurrowMicroModule: IUnitModule {
    public const string Tag = "burrow-micro-module";

    private const double BurrowDownThreshold = 0.5;
    private const double BurrowUpThreshold = 0.6;

    private readonly Unit _unit;

    public static void Install(Unit unit) {
        unit.Modules.Add(Tag, new BurrowMicroModule(unit));
    }

    public static BurrowMicroModule Uninstall(Unit worker) {
        var module = GetFrom(worker);
        worker.Modules.Remove(Tag);

        return module;
    }

    public static BurrowMicroModule GetFrom(Unit worker) {
        if (worker.Modules.TryGetValue(Tag, out var module)) {
            return module as BurrowMicroModule;
        }

        return null;
    }

    private BurrowMicroModule(Unit unit) {
        _unit = unit;
    }

    public void Execute() {
        if (!Controller.ResearchedUpgrades.Contains(Upgrades.Burrow)) {
            return;
        }

        // TODO GD Only works on roaches for now
        if (_unit.Integrity <= BurrowDownThreshold && !_unit.RawUnitData.IsBurrowed) {
            _unit.UseAbility(Abilities.BurrowRoachDown);
        }
        else if (_unit.Integrity >= BurrowUpThreshold && _unit.RawUnitData.IsBurrowed) {
            _unit.UseAbility(Abilities.BurrowRoachUp);
        }
    }
}
