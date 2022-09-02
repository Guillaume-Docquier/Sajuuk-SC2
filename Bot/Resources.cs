using Bot.GameData;

namespace Bot;

public static class Resources {
    public enum ResourceType {
        None,
        Mineral,
        Gas,
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
}
