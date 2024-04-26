namespace SC2Client.GameData;

// You can get all these values from the stableid.json file (Generally found in 'C:\Users\your_username\Documents\StarCraft II' on Windows)
public static class Abilities {
    public const uint CancelConstruction = 314;
    public const uint Cancel = 3659;
    public const uint CancelLast = 3671;
    public const uint Lift = 3679;
    public const uint Land = 3678;

    public const uint Smart = 1;
    public const uint Stop = 4;
    public const uint Attack = 23;
    public const uint Move = 16;
    public const uint Patrol = 17;
    public const uint Rally = 3673;
    public const uint Repair = 316;

    public const uint ThorSwitchAp = 2362;
    public const uint ThorSwitchNormal = 2364;
    public const uint ScannerSweep = 399;
    public const uint Yamato = 401;
    public const uint CallDownMule = 171;
    public const uint Cloak = 3676;
    public const uint ReaperGrenade = 2588;
    public const uint DepotRaise = 558;
    public const uint DepotLower = 556;
    public const uint SiegeTank = 388;
    public const uint UnsiegeTank = 390;
    public const uint TransformToHellbat = 1998;
    public const uint TransformToHellion = 1978;
    public const uint UnloadBunker = 408;
    public const uint SalvageBunker = 32;

    public const uint HarvestGather = 3666;
    private const uint SCVGather = 295;
    private const uint ProbeGather = 298;
    private const uint DroneGather = 1183;

    public static readonly HashSet<uint> Gather = new HashSet<uint>
    {
        HarvestGather,
        SCVGather,
        ProbeGather,
        DroneGather,
    };

    public const uint HarvestReturn = 3667;
    private const uint SCVReturnCargo = 296;
    private const uint ProbeReturnCargo = 299;
    private const uint DroneReturnCargo = 1184;

    public static readonly HashSet<uint> ReturnCargo = new HashSet<uint>
    {
        HarvestReturn,
        SCVReturnCargo,
        ProbeReturnCargo,
        DroneReturnCargo,
    };

    // TODO GD Each unit has its burrow command
    public const uint BurrowRoachUp = 1388;
    public const uint BurrowRoachDown = 1386;

    public const uint InjectLarvae = 251;
    public const uint SpawnCreepTumor = 3691;

    public static readonly Dictionary<uint, uint> EnergyCost = new Dictionary<uint, uint>
    {
        { InjectLarvae,    25 },
        { SpawnCreepTumor, 25 },
    };
}
