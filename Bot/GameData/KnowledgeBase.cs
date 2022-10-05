using System.Collections.Generic;
using SC2APIProtocol;

namespace Bot.GameData;

public static class KnowledgeBase {
    private static ResponseData _data;

    public const float GameGridCellWidth = 1f;
    public const float GameGridCellRadius = GameGridCellWidth / 2;
    public const int MaxSupplyAllowed = 200;

    public static ResponseData Data {
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
                else if (TechTree.MorphSource.TryGetValue(unit.UnitId, out var morpherUnitId)) {
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

    public static UnitTypeData GetUnitTypeData(uint unitType) {
        return Data.Units[(int)unitType];
    }

    public static UpgradeData GetUpgradeData(uint upgradeId) {
        return Data.Upgrades[(int)upgradeId];
    }

    public static AbilityData GetAbilityData(uint abilityId) {
        return GetAbilityData((int)abilityId);
    }

    public static AbilityData GetAbilityData(int abilityId) {
        return Data.Abilities[abilityId];
    }

    public static EffectData GetEffectData(int effectId) {
        return Data.Effects[effectId];
    }

    public static BuffData GetBuffData(int buffId) {
        return Data.Buffs[buffId];
    }
}
