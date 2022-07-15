using SC2APIProtocol;

namespace Bot.GameData;

public static class TypeData {
    private static ResponseData _data;

    public static ResponseData Data {
        get => _data;
        set {
            foreach (var unit in value.Units) {
                // The unit cost returned by the API represents the unit value.
                unit.MineralValue = unit.MineralCost;
                unit.VespeneValue = unit.VespeneCost;
            }

            foreach (var unit in value.Units) {
                if (unit.UnitId == Units.Zergling) {
                    // Zerglings must be spawned in pairs
                    unit.MineralCost *= 2;
                }
                else if (Units.MorphInto.TryGetValue(unit.UnitId, out var morpherUnitId)) {
                    // The value of a unit that is morphed from another one (e.g: all zerg units) includes the value of the morphed unit
                    // Adjust the cost to be only the extra that you need to pay
                    var morpher = value.Units[(int)morpherUnitId];
                    unit.MineralCost = unit.MineralValue - morpher.MineralValue;
                    unit.VespeneCost = unit.VespeneValue - morpher.VespeneValue;
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

    public static AbilityData GetAbilityData(int abilityId) {
        return Data.Abilities[abilityId];
    }
}
