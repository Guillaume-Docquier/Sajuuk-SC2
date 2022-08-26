namespace Bot.UnitModules;

public class KitingModule: UnitModule {
    public const string Tag = "KitingModule";

    private readonly Unit _unit;

    public static void Install(Unit unit) {
        if (PreInstallCheck(Tag, unit)) {
            unit.Modules.Add(Tag, new KitingModule(unit));
        }
    }

    private KitingModule(Unit unit) {
        _unit = unit;
    }

    protected override void DoExecute() {
        throw new System.NotImplementedException();
    }
}
