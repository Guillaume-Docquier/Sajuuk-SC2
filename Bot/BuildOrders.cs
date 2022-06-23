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
            new BuildStep(BuildType.TRAIN, 13, Units.Overlord),
            new BuildStep(BuildType.BUILD, 16, Units.Hatchery), // TODO GD Not placed on expand, no rally points
            new BuildStep(BuildType.BUILD, 18, Units.Extractor),
            new BuildStep(BuildType.BUILD, 17, Units.SpawningPool),
            new BuildStep(BuildType.TRAIN, 19, Units.Overlord),
            new BuildStep(BuildType.TRAIN, 19, Units.Queen, 2), // TODO GD Not waiting for spawning pool to be finished
            new BuildStep(BuildType.TRAIN, 24, Units.Zergling, 3), // 3 sets of 2
            new BuildStep(BuildType.TRAIN, 30, Units.Overlord),
            new BuildStep(BuildType.TRAIN, 30, Units.Queen),
            new BuildStep(BuildType.BUILD, 33, Units.Lair), // TODO GD Probably not working, blocks everything while waiting on gas but shouldn't
            new BuildStep(BuildType.BUILD, 37, Units.EvolutionChamber),
            new BuildStep(BuildType.BUILD, 37, Units.RoachWarren),
            new BuildStep(BuildType.TRAIN, 44, Units.Overlord),
            new BuildStep(BuildType.RESEARCH, 44, Abilities.ResearchZergMissileWeapons1),
            new BuildStep(BuildType.BUILD, 52, Units.Extractor, 2),
            new BuildStep(BuildType.TRAIN, 50, Units.Overlord, 2),
            new BuildStep(BuildType.RESEARCH, 50, Abilities.ResearchGlialReconstitution),
            new BuildStep(BuildType.TRAIN, 50, Units.Roach, 8),
            new BuildStep(BuildType.TRAIN, 50, Units.Roach, 1000), // Just keep going
        });
    }
}

public enum BuildType {
    TRAIN,
    BUILD,
    RESEARCH,
}
