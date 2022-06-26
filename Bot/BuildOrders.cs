using System.Collections.Generic;

namespace Bot;

using BuildOrder = Queue<BuildOrders.BuildStep>;

public static class BuildOrders {
    public class BuildStep {
        public readonly BuildType BuildType;

        public readonly uint AtSupply;

        public readonly uint UnitOrAbilityType;

        public uint Quantity;

        public BuildStep(BuildType buildType, uint atSupply, uint unitOrAbilityType, uint quantity = 1) {
            BuildType = buildType;
            AtSupply = atSupply;
            UnitOrAbilityType = unitOrAbilityType;
            Quantity = quantity;
        }
    }

    public static BuildOrder TwoBasesRoach() {
        return new BuildOrder(new[]
        {
            new BuildStep(BuildType.Train, 13, Units.Overlord),
            new BuildStep(BuildType.Build, 16, Units.Hatchery), // TODO GD Not placed on expand, no rally points
            new BuildStep(BuildType.Build, 18, Units.Extractor),
            new BuildStep(BuildType.Build, 17, Units.SpawningPool),
            new BuildStep(BuildType.Train, 19, Units.Overlord),
            new BuildStep(BuildType.Train, 19, Units.Queen, 2),
            new BuildStep(BuildType.Train, 24, Units.Zergling, 3), // 3 sets of 2
            new BuildStep(BuildType.Train, 30, Units.Overlord),
            new BuildStep(BuildType.Train, 30, Units.Queen),
            new BuildStep(BuildType.Build, 33, Units.Lair), // TODO GD Not working
            new BuildStep(BuildType.Build, 37, Units.EvolutionChamber),
            new BuildStep(BuildType.Build, 37, Units.RoachWarren),
            new BuildStep(BuildType.Train, 44, Units.Overlord),
            new BuildStep(BuildType.Research, 44, Abilities.ResearchZergMissileWeapons1),
            new BuildStep(BuildType.Build, 52, Units.Extractor, 2),
            new BuildStep(BuildType.Train, 50, Units.Overlord, 2),
            new BuildStep(BuildType.Research, 50, Abilities.ResearchGlialReconstitution),
            new BuildStep(BuildType.Train, 50, Units.Roach, 8),
            new BuildStep(BuildType.Train, 50, Units.Roach, 1000), // Just keep going
        });
    }
}

public enum BuildType {
    Train,
    Build,
    Research,
}
