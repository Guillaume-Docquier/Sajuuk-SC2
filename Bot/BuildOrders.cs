using System.Collections.Generic;
using Bot.GameData;

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

        public override string ToString() {
            var buildStepUnitOrUpgradeName = BuildType == BuildType.Research
                ? KnowledgeBase.GetUpgradeData(UnitOrUpgradeType).Name
                : $"{Quantity} {KnowledgeBase.GetUnitTypeData(UnitOrUpgradeType).Name}";

            return $"{BuildType.ToString()} {buildStepUnitOrUpgradeName} at {AtSupply} supply";
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
            new BuildStep(BuildType.Build, 37, Units.RoachWarren),
            new BuildStep(BuildType.Build, 37, Units.EvolutionChamber),
            new BuildStep(BuildType.Research, 40, Upgrades.Burrow),
            new BuildStep(BuildType.Train, 44, Units.Overlord),
            new BuildStep(BuildType.Research, 44, Upgrades.ZergMissileWeaponsLevel1),
            new BuildStep(BuildType.Build, 50, Units.Extractor, 2), // TODO GD Doesn't build on snapshot units
            new BuildStep(BuildType.Train, 50, Units.Overlord, 2),
            new BuildStep(BuildType.Research, 50, Upgrades.GlialReconstitution),
            new BuildStep(BuildType.Build, 50, Units.Extractor),
            new BuildStep(BuildType.Train, 50, Units.Roach, 8),
            new BuildStep(BuildType.Research, 1, Upgrades.TunnelingClaws),
            // All in
            new BuildStep(BuildType.Train, 1, Units.Overlord, 2),
            new BuildStep(BuildType.Train, 1, Units.Roach, 8),
            new BuildStep(BuildType.Train, 1, Units.Overlord, 5), // TODO GD Auto raise the cap
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
            new BuildStep(BuildType.Build, 17, Units.SpawningPool),
            new BuildStep(BuildType.Train, 19, Units.Overlord),
            new BuildStep(BuildType.Build, 20, Units.Extractor, 3),
            new BuildStep(BuildType.Train, 20, Units.Queen),
            new BuildStep(BuildType.Expand, 24, Units.Hatchery),
            new BuildStep(BuildType.Train, 30, Units.Overlord, 2),
            new BuildStep(BuildType.Train, 30, Units.Queen, 2),
            new BuildStep(BuildType.Train, 50, Units.Overlord),
            new BuildStep(BuildType.Train, 63, Units.Overlord),
            new BuildStep(BuildType.Train, 70, Units.Overlord, 6),
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
