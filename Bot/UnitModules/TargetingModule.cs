using System.Numerics;

namespace Bot.UnitModules;

public class TargetingModule: UnitModule {
    public const string Tag = "TargetingModule";

    private readonly Unit _unit;
    private readonly Vector3 _target;

    public static void Install(Unit unit, Vector3 target) {
        if (PreInstallCheck(Tag, unit)) {
            unit.Modules.Add(Tag, new TargetingModule(unit, target));
        }
    }

    private TargetingModule(Unit unit, Vector3 target) {
        _unit = unit;
        _target = target;
    }

    protected override void DoExecute() {
        throw new System.NotImplementedException();
    }
}
