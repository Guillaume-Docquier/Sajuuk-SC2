using System;
using System.Collections.Generic;

namespace Bot.UnitModules;

public static class UnitModule {
    private static readonly Dictionary<Type, string> Tags = new Dictionary<Type, string>()
    {
        { typeof(BurrowMicroModule), BurrowMicroModule.Tag },
        { typeof(CapacityModule), CapacityModule.Tag },
        { typeof(DebugLocationModule), DebugLocationModule.Tag },
        { typeof(KitingModule), KitingModule.Tag },
        { typeof(MiningModule), MiningModule.Tag },
        { typeof(QueenMicroModule), QueenMicroModule.Tag },
        { typeof(TargetingModule), TargetingModule.Tag },
    };

    public static T Uninstall<T>(Unit unit) where T: class, IUnitModule {
        var module = Get<T>(unit);
        if (module != null) {
            unit.Modules.Remove(Tags[typeof(T)]);
        }

        return module;
    }

    public static T Get<T>(Unit unit) where T: class, IUnitModule {
        if (unit == null) {
            return null;
        }

        if (unit.Modules.TryGetValue(Tags[typeof(T)], out var module)) {
            return module as T;
        }

        return null;
    }
}
