using Bot.UnitModules;

namespace Bot;

public static class UnitUtils {
    public enum ResourceType {
        Mineral,
        Gas,
        None,
    }

    public static ResourceType GetResourceType(Unit resource) {
        if (Units.MineralFields.Contains(resource.UnitType)) {
            return ResourceType.Mineral;
        }

        if (Units.Extractors.Contains(resource.UnitType) || Units.GasGeysers.Contains(resource.UnitType)) {
            return ResourceType.Gas;
        }

        return ResourceType.None;
    }

    public static bool IsResourceManaged(Unit resource) {
        return resource.Modules.ContainsKey(CapacityModule.Tag);
    }

    public static bool IsGasExploited(Unit gas) {
        return IsResourceManaged(gas) && CapacityModule.GetAvailableCapacity(gas) == 0;
    }
}
