namespace Bot.UnitModules;

public class KitingModule: IUnitModule {
    public const string Tag = "kiting-module";

    private readonly Unit _unit;

    public static void Install(Unit unit) {
        if (UnitModule.PreInstallCheck(Tag, unit)) {
            unit.Modules.Add(Tag, new KitingModule(unit));
        }
    }

    private KitingModule(Unit unit) {
        _unit = unit;
    }

    public void Execute() {
        throw new System.NotImplementedException();
    }
}
