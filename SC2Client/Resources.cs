using SC2Client.GameData;
using SC2Client.State;

namespace SC2Client;

public static class Resources {
    public const int MaxDronesPerExtractor = 3;
    public const int IdealDronesPerMinerals = 2;
    public const int MaxDronesPerMinerals = 3;

    public enum ResourceType {
        None,
        Mineral,
        Gas,
    }

    public static ResourceType GetResourceType(IUnit resource) {
        if (UnitTypeId.MineralFields.Contains(resource.UnitType)) {
            return ResourceType.Mineral;
        }

        if (UnitTypeId.Extractors.Contains(resource.UnitType) || UnitTypeId.GasGeysers.Contains(resource.UnitType)) {
            return ResourceType.Gas;
        }

        return ResourceType.None;
    }
}
