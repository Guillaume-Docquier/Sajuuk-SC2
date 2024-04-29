namespace SC2Client.GameData;

// You can get all these values from the stableid.json file (Generally found in 'C:\Users\your_username\Documents\StarCraft II' on Windows)
public static class BuffId {
    public const uint OnCreepSomething = 303; // Not sure what this is
    public const uint OnCreepVisible = 306;

    public static readonly HashSet<uint> OnCreep = new HashSet<uint>
    {
        OnCreepSomething,
        OnCreepVisible,
    };

    public const uint CarryMineralFieldMinerals = 271;
    public const uint CarryHighYieldMineralFieldMinerals = 272;

    public static readonly HashSet<uint> CarryMinerals = new HashSet<uint>
    {
        CarryMineralFieldMinerals,
        CarryHighYieldMineralFieldMinerals,
    };
}
