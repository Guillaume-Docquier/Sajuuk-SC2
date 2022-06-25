using SC2APIProtocol;

namespace Bot.Wrapper;

public static class GameData {
    private static ResponseData _data; // TODO GD Refine this e.g. modify zerg unit costs

    public static ResponseData Data {
        get => _data;
        set {
            _data = value;
        }
    }

    public static UnitTypeData GetUnitTypeData(uint unitType) {
        return Data.Units[(int)unitType];
    }
}
