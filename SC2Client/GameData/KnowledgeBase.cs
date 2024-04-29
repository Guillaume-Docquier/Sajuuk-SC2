using SC2APIProtocol;

namespace SC2Client.GameData;

/// <summary>
/// Contains static data about SC2 units, abilities, buffs, etc.
/// </summary>
public class KnowledgeBase {
    // TODO GD Can this be inferred from the game data?
    private readonly Dictionary<uint, uint> _morphSource = new Dictionary<uint, uint>
    {
        { UnitTypeId.Drone,            UnitTypeId.Larva },
        { UnitTypeId.Corruptor,        UnitTypeId.Larva },
        { UnitTypeId.BroodLord,        UnitTypeId.Corruptor },
        { UnitTypeId.Hydralisk,        UnitTypeId.Larva },
        { UnitTypeId.Lurker,           UnitTypeId.Hydralisk },
        { UnitTypeId.Infestor,         UnitTypeId.Larva },
        { UnitTypeId.Mutalisk,         UnitTypeId.Larva },
        { UnitTypeId.Overlord,         UnitTypeId.Larva },
        { UnitTypeId.Overseer,         UnitTypeId.Overlord },
        { UnitTypeId.Roach,            UnitTypeId.Larva },
        { UnitTypeId.Ravager,          UnitTypeId.Roach },
        { UnitTypeId.Ultralisk,        UnitTypeId.Larva },
        { UnitTypeId.Zergling,         UnitTypeId.Larva },
        { UnitTypeId.SwarmHost,        UnitTypeId.Larva },
        { UnitTypeId.Viper,            UnitTypeId.Larva },
        { UnitTypeId.Baneling,         UnitTypeId.Zergling },
        { UnitTypeId.BanelingNest,     UnitTypeId.Drone },
        { UnitTypeId.EvolutionChamber, UnitTypeId.Drone },
        { UnitTypeId.Extractor,        UnitTypeId.Drone },
        { UnitTypeId.Hatchery,         UnitTypeId.Drone },
        { UnitTypeId.Lair,             UnitTypeId.Hatchery },
        { UnitTypeId.Hive,             UnitTypeId.Lair },
        { UnitTypeId.HydraliskDen,     UnitTypeId.Drone },
        { UnitTypeId.LurkerDen,        UnitTypeId.Drone },
        { UnitTypeId.InfestationPit,   UnitTypeId.Drone },
        { UnitTypeId.NydusNetwork,     UnitTypeId.Drone },
        { UnitTypeId.RoachWarren,      UnitTypeId.Drone },
        { UnitTypeId.SpawningPool,     UnitTypeId.Drone },
        { UnitTypeId.SpineCrawler,     UnitTypeId.Drone },
        { UnitTypeId.Spire,            UnitTypeId.Drone },
        { UnitTypeId.GreaterSpire,     UnitTypeId.Spire },
        { UnitTypeId.SporeCrawler,     UnitTypeId.Drone },
        { UnitTypeId.UltraliskCavern,  UnitTypeId.Drone },
    };

    public KnowledgeBase(ResponseData data) {
        // We will not be able to get the real resource values after this point.
        // I used to hack the proto files but I don't think that's a good idea
        // However, I've never needed it so far, so we'll cross that bridge when we get there.
        var unitValues = new Dictionary<uint, (uint Mineral, uint Vespene)>();
        foreach (var unit in data.Units) {
            // The unit cost returned by the API represents the unit value.
            unitValues[unit.UnitId] = (unit.MineralCost, unit.VespeneCost);
        }

        foreach (var unit in data.Units) {
            if (unit.UnitId == UnitTypeId.Zergling) {
                // Zerglings must be spawned in pairs
                unit.MineralCost *= 2;
            }
            else if (_morphSource.TryGetValue(unit.UnitId, out var morpherUnitId)) {
                // The value of a unit that is morphed from another one (e.g: all zerg units) includes the value of the morphed unit
                // Adjust the cost to be only the extra that you need to pay
                var morpher = data.Units[(int)morpherUnitId];
                unit.MineralCost = unitValues[unit.UnitId].Mineral - unitValues[morpher.UnitId].Mineral;
                unit.VespeneCost = unitValues[unit.UnitId].Vespene - unitValues[morpher.UnitId].Vespene;
            }
        }

        _data = data;
    }

    private readonly ResponseData _data;

    public const float GameGridCellWidth = 1f;
    public const float GameGridCellRadius = GameGridCellWidth / 2;
    public const int MaxSupplyAllowed = 200;

    public UnitTypeData GetUnitTypeData(uint unitType) {
        return _data.Units[(int)unitType];
    }

    public UpgradeData GetUpgradeData(uint upgradeId) {
        return _data.Upgrades[(int)upgradeId];
    }

    public AbilityData GetAbilityData(uint abilityId) {
        return GetAbilityData((int)abilityId);
    }

    public AbilityData GetAbilityData(int abilityId) {
        return _data.Abilities[abilityId];
    }

    public EffectData GetEffectData(uint effectId) {
        return GetEffectData((int)effectId);
    }

    public EffectData GetEffectData(int effectId) {
        return _data.Effects[effectId];
    }

    public BuffData GetBuffData(int buffId) {
        return _data.Buffs[buffId];
    }

    public float GetBuildingRadius(uint buildingType) {
        return GetAbilityData(GetUnitTypeData(buildingType).AbilityId).FootprintRadius;
    }
}
