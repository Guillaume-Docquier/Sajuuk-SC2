using System.Collections.Generic;

namespace Bot.GameData;

public class TechTree {
    public readonly Dictionary<uint, uint> Producer = new Dictionary<uint, uint>
    {
        { Units.Drone,                       Units.Larva },
        { Units.Corruptor,                   Units.Larva },
        { Units.BroodLord,                   Units.Corruptor },
        { Units.Hydralisk,                   Units.Larva },
        { Units.Lurker,                      Units.Hydralisk },
        { Units.Infestor,                    Units.Larva },
        { Units.Mutalisk,                    Units.Larva },
        { Units.Overlord,                    Units.Larva },
        { Units.Overseer,                    Units.Overlord },
        { Units.Queen,                       Units.Hatchery },
        { Units.Roach,                       Units.Larva },
        { Units.Ravager,                     Units.Roach },
        { Units.Ultralisk,                   Units.Larva },
        { Units.Zergling,                    Units.Larva },
        { Units.SwarmHost,                   Units.Larva },
        { Units.Viper,                       Units.Larva },
        { Units.Baneling,                    Units.Zergling },
        { Units.BanelingNest,                Units.Drone },
        { Units.EvolutionChamber,            Units.Drone },
        { Units.Extractor,                   Units.Drone },
        { Units.Hatchery,                    Units.Drone },
        { Units.Lair,                        Units.Hatchery },
        { Units.Hive,                        Units.Lair },
        { Units.HydraliskDen,                Units.Drone },
        { Units.LurkerDen,                   Units.HydraliskDen },
        { Units.InfestationPit,              Units.Drone },
        { Units.NydusNetwork,                Units.Drone },
        { Units.RoachWarren,                 Units.Drone },
        { Units.SpawningPool,                Units.Drone },
        { Units.SpineCrawler,                Units.Drone },
        { Units.Spire,                       Units.Drone },
        { Units.GreaterSpire,                Units.Spire },
        { Units.SporeCrawler,                Units.Drone },
        { Units.UltraliskCavern,             Units.Drone },

        { Upgrades.ZergMissileWeaponsLevel1, Units.EvolutionChamber },
        { Upgrades.ZergMissileWeaponsLevel2, Units.EvolutionChamber },
        { Upgrades.ZergMissileWeaponsLevel3, Units.EvolutionChamber },
        { Upgrades.ZergMeleeWeaponsLevel1,   Units.EvolutionChamber },
        { Upgrades.ZergMeleeWeaponsLevel2,   Units.EvolutionChamber },
        { Upgrades.ZergMeleeWeaponsLevel3,   Units.EvolutionChamber },
        { Upgrades.ZergGroundArmorsLevel1,   Units.EvolutionChamber },
        { Upgrades.ZergGroundArmorsLevel2,   Units.EvolutionChamber },
        { Upgrades.ZergGroundArmorsLevel3,   Units.EvolutionChamber },
        { Upgrades.GlialReconstitution,      Units.RoachWarren },
        { Upgrades.TunnelingClaws,           Units.RoachWarren },
        { Upgrades.Burrow,                   Units.Hatchery },
    };

    public readonly Dictionary<uint, List<IPrerequisite>> UnitPrerequisites;
    public readonly Dictionary<uint, List<IPrerequisite>> UpgradePrerequisites;

    public TechTree(IPrerequisiteFactory prerequisiteFactory) {
        UnitPrerequisites = new Dictionary<uint, List<IPrerequisite>>
        {
            { Units.EvolutionChamber,            new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.Hatchery) }},
            { Units.SpawningPool,                new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.Hatchery) }},
            { Units.Zergling,                    new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.SpawningPool) }},
            { Units.Queen,                       new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.SpawningPool) }},
            { Units.SpineCrawler,                new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.SpawningPool) }},
            { Units.SporeCrawler,                new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.SpawningPool) }},
            { Units.BanelingNest,                new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.SpawningPool) }},
            { Units.Baneling,                    new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.BanelingNest) }},
            { Units.RoachWarren,                 new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.SpawningPool) }},
            { Units.Roach,                       new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.RoachWarren) }},
            { Units.Ravager,                     new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.RoachWarren) }},
            { Units.Lair,                        new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.SpawningPool) }},
            { Units.Overseer,                    new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.Lair) }},
            { Units.NydusNetwork,                new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.Lair) }},
            { Units.NydusWorm,                   new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.NydusNetwork) }},
            { Units.InfestationPit,              new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.Lair) }},
            { Units.Infestor,                    new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.InfestationPit) }},
            { Units.SwarmHost,                   new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.InfestationPit) }},
            { Units.Spire,                       new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.Lair) }},
            { Units.Corruptor,                   new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.Spire) }},
            { Units.Mutalisk,                    new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.Spire) }},
            { Units.HydraliskDen,                new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.Lair) }},
            { Units.Hydralisk,                   new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.HydraliskDen) }},
            { Units.LurkerDen,                   new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.HydraliskDen) }},
            { Units.Lurker,                      new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.LurkerDen) }},
            { Units.Hive,                        new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.InfestationPit) }},
            { Units.Viper,                       new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.Hive) }},
            { Units.GreaterSpire,                new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.Hive) }},
            { Units.BroodLord,                   new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.GreaterSpire) }},
            { Units.UltraliskCavern,             new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.Hive) }},
            { Units.Ultralisk,                   new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.UltraliskCavern) }},
        };

        UpgradePrerequisites = new Dictionary<uint, List<IPrerequisite>>
        {
            { Upgrades.ZergMissileWeaponsLevel1, new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.EvolutionChamber) }},
            { Upgrades.ZergMissileWeaponsLevel2, new List<IPrerequisite> { prerequisiteFactory.CreateTechPrerequisite(Upgrades.ZergMissileWeaponsLevel1), prerequisiteFactory.CreateUnitPrerequisite(Units.Lair) }},
            { Upgrades.ZergMissileWeaponsLevel3, new List<IPrerequisite> { prerequisiteFactory.CreateTechPrerequisite(Upgrades.ZergMissileWeaponsLevel2), prerequisiteFactory.CreateUnitPrerequisite(Units.Hive) }},
            { Upgrades.ZergMeleeWeaponsLevel1,   new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.EvolutionChamber) }},
            { Upgrades.ZergMeleeWeaponsLevel2,   new List<IPrerequisite> { prerequisiteFactory.CreateTechPrerequisite(Upgrades.ZergMeleeWeaponsLevel1), prerequisiteFactory.CreateUnitPrerequisite(Units.Lair) }},
            { Upgrades.ZergMeleeWeaponsLevel3,   new List<IPrerequisite> { prerequisiteFactory.CreateTechPrerequisite(Upgrades.ZergMeleeWeaponsLevel2), prerequisiteFactory.CreateUnitPrerequisite(Units.Hive) }},
            { Upgrades.ZergGroundArmorsLevel1,   new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.EvolutionChamber) }},
            { Upgrades.ZergGroundArmorsLevel2,   new List<IPrerequisite> { prerequisiteFactory.CreateTechPrerequisite(Upgrades.ZergGroundArmorsLevel1), prerequisiteFactory.CreateUnitPrerequisite(Units.Lair) }},
            { Upgrades.ZergGroundArmorsLevel3,   new List<IPrerequisite> { prerequisiteFactory.CreateTechPrerequisite(Upgrades.ZergGroundArmorsLevel2), prerequisiteFactory.CreateUnitPrerequisite(Units.Hive) }},
            { Upgrades.GlialReconstitution,      new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.RoachWarren), prerequisiteFactory.CreateUnitPrerequisite(Units.Lair) }},
            { Upgrades.TunnelingClaws,           new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.RoachWarren), prerequisiteFactory.CreateUnitPrerequisite(Units.Lair) }},
            { Upgrades.Burrow,                   new List<IPrerequisite> { prerequisiteFactory.CreateUnitPrerequisite(Units.Hatchery) }},
        };
    }
}
