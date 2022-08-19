using System.Collections.Generic;
using Bot.GameData;

namespace Bot.Builds;

using BuildOrder = LinkedList<BuildRequest>;

public static class BuildOrders {
    public static BuildOrder TwoBasesRoach() {
        return new BuildOrder(new BuildRequest[]
        {
            new QuantityBuildRequest(BuildType.Train,       Units.Overlord,                    atSupply: 13),
            new QuantityBuildRequest(BuildType.Expand,      Units.Hatchery,                    atSupply: 16),
            new QuantityBuildRequest(BuildType.Build,       Units.Extractor,                   atSupply: 18),
            new QuantityBuildRequest(BuildType.Build,       Units.SpawningPool,                atSupply: 17),
            new QuantityBuildRequest(BuildType.Train,       Units.Overlord,                    atSupply: 19),
            new TargetBuildRequest(BuildType.Train,         Units.Queen,                       atSupply: 19, targetQuantity: 2),
            new QuantityBuildRequest(BuildType.Train,       Units.Zergling,                    atSupply: 24, quantity: 3),
            new QuantityBuildRequest(BuildType.Train,       Units.Overlord,                    atSupply: 30),
            new TargetBuildRequest(BuildType.Train,         Units.Queen,                       atSupply: 30, targetQuantity: 3),
            new QuantityBuildRequest(BuildType.UpgradeInto, Units.Lair,                        atSupply: 33),
            new QuantityBuildRequest(BuildType.Build,       Units.RoachWarren,                 atSupply: 37),
            new QuantityBuildRequest(BuildType.Build,       Units.EvolutionChamber,            atSupply: 37),
            new QuantityBuildRequest(BuildType.Research,    Upgrades.Burrow,                   atSupply: 40),
            new QuantityBuildRequest(BuildType.Train,       Units.Overlord,                    atSupply: 44),
            new QuantityBuildRequest(BuildType.Research,    Upgrades.ZergMissileWeaponsLevel1, atSupply: 44),
            new QuantityBuildRequest(BuildType.Build,       Units.Extractor,                   atSupply: 50, quantity: 2), // TODO GD Doesn't build on snapshot units
            new QuantityBuildRequest(BuildType.Train,       Units.Overlord,                    atSupply: 50, quantity: 2),
            new QuantityBuildRequest(BuildType.Research,    Upgrades.TunnelingClaws,           atSupply: 50),
            new QuantityBuildRequest(BuildType.Build,       Units.Extractor,                   atSupply: 50),
            new QuantityBuildRequest(BuildType.Train,       Units.Roach,                       atSupply: 50, quantity: 8),
            new QuantityBuildRequest(BuildType.Research,    Upgrades.GlialReconstitution),
            // All in
            new QuantityBuildRequest(BuildType.Train, Units.Overlord, quantity: 2),
            new QuantityBuildRequest(BuildType.Train, Units.Roach,    quantity: 8),
            new QuantityBuildRequest(BuildType.Train, Units.Overlord, quantity: 5),
        });
    }

    public static BuildOrder TestGasMining() {
        return new BuildOrder(new[]
        {
            new QuantityBuildRequest(BuildType.Build, Units.Extractor),
            new QuantityBuildRequest(BuildType.Train, Units.Overlord, atSupply: 13),
            new QuantityBuildRequest(BuildType.Build, Units.Extractor),
            new QuantityBuildRequest(BuildType.Train, Units.Overlord, atSupply: 19),
            new QuantityBuildRequest(BuildType.Train, Units.Overlord, atSupply: 19),
        });
    }

    public static BuildOrder TestExpands() {
        return new BuildOrder(new[]
        {
            new QuantityBuildRequest(BuildType.Train,  Units.Overlord,     atSupply: 13),
            new QuantityBuildRequest(BuildType.Build,  Units.Extractor,    atSupply: 16),
            new QuantityBuildRequest(BuildType.Expand, Units.Hatchery,     atSupply: 16),
            new QuantityBuildRequest(BuildType.Build,  Units.SpawningPool, atSupply: 17),
            new QuantityBuildRequest(BuildType.Train,  Units.Overlord,     atSupply: 19),
            new QuantityBuildRequest(BuildType.Build,  Units.Extractor,    atSupply: 20, quantity: 3),
            new QuantityBuildRequest(BuildType.Train,  Units.Queen,        atSupply: 20),
            new QuantityBuildRequest(BuildType.Expand, Units.Hatchery,     atSupply: 24),
            new QuantityBuildRequest(BuildType.Train,  Units.Overlord,     atSupply: 30, quantity: 2),
            new QuantityBuildRequest(BuildType.Train,  Units.Queen,        atSupply: 30, quantity: 2),
            new QuantityBuildRequest(BuildType.Train,  Units.Overlord,     atSupply: 50),
            new QuantityBuildRequest(BuildType.Train,  Units.Overlord,     atSupply: 63),
            new QuantityBuildRequest(BuildType.Train,  Units.Overlord,     atSupply: 70, quantity: 6),
        });
    }

    public static BuildOrder TestSpeedMining() {
        return new BuildOrder(new[]
        {
            new QuantityBuildRequest(BuildType.Train, Units.Overlord, atSupply: 13),
            // This will block the build
            new QuantityBuildRequest(BuildType.Train, Units.Queen, atSupply: 16),
        });
    }
}

public enum BuildType {
    Train,
    Build,
    Research,
    UpgradeInto,
    Expand,
}
