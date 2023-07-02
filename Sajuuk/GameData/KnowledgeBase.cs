using System.Collections.Generic;
using SC2APIProtocol;

namespace Sajuuk.GameData;

public class KnowledgeBase {
    // TODO GD Can this be inferred from the game data?
    private readonly Dictionary<uint, uint> _morphSource = new Dictionary<uint, uint>
    {
        { Units.Drone,            Units.Larva },
        { Units.Corruptor,        Units.Larva },
        { Units.BroodLord,        Units.Corruptor },
        { Units.Hydralisk,        Units.Larva },
        { Units.Lurker,           Units.Hydralisk },
        { Units.Infestor,         Units.Larva },
        { Units.Mutalisk,         Units.Larva },
        { Units.Overlord,         Units.Larva },
        { Units.Overseer,         Units.Overlord },
        { Units.Roach,            Units.Larva },
        { Units.Ravager,          Units.Roach },
        { Units.Ultralisk,        Units.Larva },
        { Units.Zergling,         Units.Larva },
        { Units.SwarmHost,        Units.Larva },
        { Units.Viper,            Units.Larva },
        { Units.Baneling,         Units.Zergling },
        { Units.BanelingNest,     Units.Drone },
        { Units.EvolutionChamber, Units.Drone },
        { Units.Extractor,        Units.Drone },
        { Units.Hatchery,         Units.Drone },
        { Units.Lair,             Units.Hatchery },
        { Units.Hive,             Units.Lair },
        { Units.HydraliskDen,     Units.Drone },
        { Units.LurkerDen,        Units.Drone },
        { Units.InfestationPit,   Units.Drone },
        { Units.NydusNetwork,     Units.Drone },
        { Units.RoachWarren,      Units.Drone },
        { Units.SpawningPool,     Units.Drone },
        { Units.SpineCrawler,     Units.Drone },
        { Units.Spire,            Units.Drone },
        { Units.GreaterSpire,     Units.Spire },
        { Units.SporeCrawler,     Units.Drone },
        { Units.UltraliskCavern,  Units.Drone },
    };

    private ResponseData _data;

    public const float GameGridCellWidth = 1f;
    public const float GameGridCellRadius = GameGridCellWidth / 2;
    public const int MaxSupplyAllowed = 200;

    public ResponseData Data {
        get => _data;
        set {
            if (Program.DebugEnabled) {
                // We save the ResponseData to load it during tests
                KnowledgeBaseDataStore.Save(value);
            }

            // We will not be able to get the real resource values after this point.
            // I used to hack the proto files but I don't think that's a good idea
            // However, I've never needed it so far, so we'll cross that bridge when we get there.
            var unitValues = new Dictionary<uint, (uint Mineral, uint Vespene)>();
            foreach (var unit in value.Units) {
                // The unit cost returned by the API represents the unit value.
                unitValues[unit.UnitId] = (unit.MineralCost, unit.VespeneCost);
            }

            foreach (var unit in value.Units) {
                if (unit.UnitId == Units.Zergling) {
                    // Zerglings must be spawned in pairs
                    unit.MineralCost *= 2;
                }
                else if (_morphSource.TryGetValue(unit.UnitId, out var morpherUnitId)) {
                    // The value of a unit that is morphed from another one (e.g: all zerg units) includes the value of the morphed unit
                    // Adjust the cost to be only the extra that you need to pay
                    var morpher = value.Units[(int)morpherUnitId];
                    unit.MineralCost = unitValues[unit.UnitId].Mineral - unitValues[morpher.UnitId].Mineral;
                    unit.VespeneCost = unitValues[unit.UnitId].Vespene - unitValues[morpher.UnitId].Vespene;
                }
            }

            _data = value;
        }
    }

    public UnitTypeData GetUnitTypeData(uint unitType) {
        return Data.Units[(int)unitType];
    }

    public UpgradeData GetUpgradeData(uint upgradeId) {
        return Data.Upgrades[(int)upgradeId];
    }

    public AbilityData GetAbilityData(uint abilityId) {
        return GetAbilityData((int)abilityId);
    }

    public AbilityData GetAbilityData(int abilityId) {
        return Data.Abilities[abilityId];
    }

    public EffectData GetEffectData(uint effectId) {
        return GetEffectData((int)effectId);
    }

    public EffectData GetEffectData(int effectId) {
        return Data.Effects[effectId];
    }

    public BuffData GetBuffData(int buffId) {
        return Data.Buffs[buffId];
    }

    public float GetBuildingRadius(uint buildingType) {
        return GetAbilityData(GetUnitTypeData(buildingType).AbilityId).FootprintRadius;
    }
}
