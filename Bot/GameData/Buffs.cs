using System.Collections.Generic;

namespace Bot.GameData;

// You can get all these values from the stableid.json file (Generally found in 'C:\Users\your_username\Documents\StarCraft II' on Windows)
internal static class Buffs {
    public const uint OnCreep = 303;

    public const uint CarryMineralFieldMinerals = 271;
    public const uint CarryHighYieldMineralFieldMinerals = 272;

    public static readonly HashSet<uint> CarryMinerals = new HashSet<uint>
    {
        CarryMineralFieldMinerals,
        CarryHighYieldMineralFieldMinerals,
    };
}
