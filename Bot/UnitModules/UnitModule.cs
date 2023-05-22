using System;
using System.Collections.Generic;

namespace Bot.UnitModules;

public abstract class UnitModule: IUnitModule {
    private bool _isModuleEnabled = true;

    public readonly string Tag;

    protected UnitModule(string tag) {
        Tag = tag;
    }

    public void Enable() {
        _isModuleEnabled = true;
    }

    public void Disable() {
        _isModuleEnabled = false;
    }

    public bool Execute() {
        if (_isModuleEnabled) {
            DoExecute();

            return true;
        }

        return false;
    }

    protected virtual void OnUninstall() {}

    protected abstract void DoExecute();

    private static readonly Dictionary<Type, string> ModuleTags = new Dictionary<Type, string>()
    {
        { typeof(AttackPriorityModule), AttackPriorityModule.ModuleTag },
        { typeof(CapacityModule), CapacityModule.ModuleTag },
        { typeof(ChangelingTargetingModule), ChangelingTargetingModule.ModuleTag },
        { typeof(DebugLocationModule), DebugLocationModule.ModuleTag },
        { typeof(MiningModule), MiningModule.ModuleTag },
        { typeof(QueenMicroModule), QueenMicroModule.ModuleTag },
        { typeof(TumorCreepSpreadModule), TumorCreepSpreadModule.ModuleTag },
    };

    public static T Uninstall<T>(Unit unit) where T: UnitModule {
        var module = Get<T>(unit);
        if (module != null) {
            unit.Modules.Remove(module.Tag);
            module.OnUninstall();
        }

        return module;
    }

    public static T Get<T>(Unit unit) where T: UnitModule {
        if (unit == null) {
            Logger.Error($"Trying to get the {typeof(T)} module of a unit, but the unit is null");
            return null;
        }

        if (unit.Modules.TryGetValue(ModuleTags[typeof(T)], out var module)) {
            return module as T;
        }

        return null;
    }
}
