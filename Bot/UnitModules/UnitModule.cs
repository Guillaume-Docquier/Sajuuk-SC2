using System;
using System.Collections.Generic;

namespace Bot.UnitModules;

public abstract class UnitModule: IUnitModule {
    private bool _isModuleEnabled = true;

    public void Enable() {
        _isModuleEnabled = true;
    }

    public void Disable() {
        _isModuleEnabled = false;
    }

    public void Execute() {
        if (_isModuleEnabled) {
            DoExecute();
        }
    }

    protected virtual void OnUninstall() {}

    protected abstract void DoExecute();

    private static readonly Dictionary<Type, string> Tags = new Dictionary<Type, string>()
    {
        { typeof(AttackPriorityModule), AttackPriorityModule.Tag },
        { typeof(BurrowMicroModule), BurrowMicroModule.Tag },
        { typeof(CapacityModule), CapacityModule.Tag },
        { typeof(ChangelingTargetingModule), ChangelingTargetingModule.Tag },
        { typeof(DebugLocationModule), DebugLocationModule.Tag },
        { typeof(KitingModule), KitingModule.Tag },
        { typeof(MiningModule), MiningModule.Tag },
        { typeof(QueenMicroModule), QueenMicroModule.Tag },
        { typeof(TargetingModule), TargetingModule.Tag },
        { typeof(TargetNeutralUnitsModule), TargetNeutralUnitsModule.Tag },
        { typeof(TumorCreepSpreadModule), TumorCreepSpreadModule.Tag },
    };

    public static bool PreInstallCheck(string moduleTag, Unit unit) {
        if (unit == null) {
            Logger.Error("Pre-install: Unit was null when trying to install {0}.", moduleTag);

            return false;
        }

        if (unit.Modules.Remove(moduleTag)) {
            Logger.Error("Pre-install: Removed {0} from {1}.", moduleTag, unit);
        }

        return true;
    }

    public static T Uninstall<T>(Unit unit) where T: UnitModule {
        var module = Get<T>(unit);
        if (module != null) {
            unit.Modules.Remove(Tags[typeof(T)]);
            module.OnUninstall();
        }

        return module;
    }

    public static T Get<T>(Unit unit) where T: UnitModule {
        if (unit == null) {
            return null;
        }

        if (unit.Modules.TryGetValue(Tags[typeof(T)], out var module)) {
            return module as T;
        }

        return null;
    }
}
