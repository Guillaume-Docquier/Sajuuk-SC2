using System.Collections.Generic;

namespace Bot;

using BuildOrder = Queue<BuildOrders.BuildStep>;

public static class BuildOrders {
    public class BuildStep {
        public readonly BuildType BuildType;

        public readonly uint AtSupply;

        public readonly uint UnitOrUpgradeType;

        public uint Quantity;

        public BuildStep(BuildType buildType, uint atSupply, uint unitOrUpgradeType, uint quantity = 1) {
            BuildType = buildType;
            AtSupply = atSupply;
            UnitOrUpgradeType = unitOrUpgradeType;
            Quantity = quantity;
        }
    }

    public static BuildOrder TwoBasesRoach() {
        return new BuildOrder(new[]
        {
            new BuildStep(BuildType.Train, 13, Units.Overlord),
            new BuildStep(BuildType.Expand, 16, Units.Hatchery),
            new BuildStep(BuildType.Build, 18, Units.Extractor),
            new BuildStep(BuildType.Build, 17, Units.SpawningPool),
            new BuildStep(BuildType.Train, 19, Units.Overlord),
            new BuildStep(BuildType.Train, 19, Units.Queen, 2),
            new BuildStep(BuildType.Train, 24, Units.Zergling, 3),
            new BuildStep(BuildType.Train, 30, Units.Overlord),
            new BuildStep(BuildType.Train, 30, Units.Queen),
            new BuildStep(BuildType.UpgradeInto, 33, Units.Lair),
            new BuildStep(BuildType.Build, 37, Units.EvolutionChamber),
            new BuildStep(BuildType.Build, 37, Units.RoachWarren),
            new BuildStep(BuildType.Train, 44, Units.Overlord),
            new BuildStep(BuildType.Research, 44, Upgrades.ZergMissileWeaponsLevel1),
            new BuildStep(BuildType.Build, 52, Units.Extractor, 2),
            new BuildStep(BuildType.Train, 50, Units.Overlord, 2),
            new BuildStep(BuildType.Research, 50, Upgrades.GlialReconstitution),
            new BuildStep(BuildType.Train, 50, Units.Roach, 8),
            new BuildStep(BuildType.Train, 50, Units.Roach, 1000), // Just keep going
        });
    }

    public static BuildOrder TestGasMining() {
        return new BuildOrder(new[]
        {
            new BuildStep(BuildType.Build, 1, Units.Extractor),
            new BuildStep(BuildType.Train, 13, Units.Overlord),
            new BuildStep(BuildType.Build, 1, Units.Extractor),
            new BuildStep(BuildType.Train, 19, Units.Overlord),
            new BuildStep(BuildType.Train, 19, Units.Overlord),
        });
    }

    public static BuildOrder TestExpands() {
        return new BuildOrder(new[]
        {
            new BuildStep(BuildType.Train, 13, Units.Overlord),
            new BuildStep(BuildType.Build, 16, Units.Extractor),
            new BuildStep(BuildType.Expand, 16, Units.Hatchery),
            new BuildStep(BuildType.Build, 20, Units.Extractor, 3),
            new BuildStep(BuildType.Train, 19, Units.Overlord),
            new BuildStep(BuildType.Expand, 24, Units.Hatchery),
            new BuildStep(BuildType.Train, 30, Units.Overlord, 2),
            new BuildStep(BuildType.Train, 53, Units.Overlord),
            new BuildStep(BuildType.Train, 63, Units.Overlord),
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
