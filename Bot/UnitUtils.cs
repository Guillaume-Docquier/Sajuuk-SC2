using Bot.UnitModules;

namespace Bot;

public static class UnitUtils {
    public static bool IsResourceManaged(Unit resource) {
        return resource.Modules.ContainsKey(CapacityModule.GlobalTag);
    }

    public static bool IsGasExploited(Unit gas) {
        return IsResourceManaged(gas) && CapacityModule.GetAvailableCapacity(gas) == 0;
    }
}
