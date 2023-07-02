using Sajuuk.GameData;

namespace Sajuuk;

public static class Resources {
    public const int MaxDronesPerExtractor = 3;
    public const int IdealDronesPerMinerals = 2;
    public const int MaxDronesPerMinerals = 3;

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
