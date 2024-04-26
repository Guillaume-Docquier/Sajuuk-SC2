namespace SC2Client.GameData;

public static class Units {
    #region UnitTypeIds

    public const uint Colossus = 4;
    public const uint Techlab = 5;
    public const uint Reactor = 6;
    public const uint InfestorTerran = 7;
    public const uint BanelingCocoon = 8;
    public const uint Baneling = 9;
    public const uint Mothership = 10;
    public const uint PointDefenseDrone = 11;
    public const uint Changeling = 12;
    public const uint ChangelingZealot = 13;
    public const uint ChangelingMarineShield = 14;
    public const uint ChangelingMarine = 15;
    public const uint ChangelingZerglingWings = 16;
    public const uint ChangelingZergling = 17;
    public const uint CommandCenter = 18;
    public const uint SupplyDepot = 19;
    public const uint SupplyDepotLowered = 47;
    public const uint Refinery = 20;
    public const uint Barracks = 21;
    public const uint EngineeringBay = 22;
    public const uint MissileTurret = 23;
    public const uint Bunker = 24;
    public const uint SensorTower = 25;
    public const uint GhostAcademy = 26;
    public const uint Factory = 27;
    public const uint Starport = 28;
    public const uint Armory = 29;
    public const uint FusionCore = 30;
    public const uint AutoTurret = 31;
    public const uint SiegeTankSieged = 32;
    public const uint SiegeTank = 33;
    public const uint VikingAssault = 34;
    public const uint VikingFighter = 35;
    public const uint CommandCenterFlying = 36;
    public const uint BarracksTechlab = 37;
    public const uint BarracksReactor = 38;
    public const uint FactoryTechlab = 39;
    public const uint FactoryReactor = 40;
    public const uint StarportTechlab = 41;
    public const uint StarportReactor = 42;
    public const uint FactoryFlying = 43;
    public const uint StarportFlying = 44;
    public const uint Scv = 45;
    public const uint BarracksFlying = 46;
    public const uint Marine = 48;
    public const uint Reaper = 49;
    public const uint WidowMine = 498;
    public const uint WidowMineBurrowed = 500;
    public const uint Liberator = 689;
    public const uint Ghost = 50;
    public const uint Marauder = 51;
    public const uint Mule = 268;
    public const uint Thor = 52;
    public const uint Hellion = 53;
    public const uint Hellbat = 484;
    public const uint Cyclone = 692;
    public const uint Medivac = 54;
    public const uint Banshee = 55;
    public const uint Raven = 56;
    public const uint Battlecruiser = 57;
    public const uint Nuke = 58;
    public const uint Nexus = 59;
    public const uint Pylon = 60;
    public const uint Assimilator = 61;
    public const uint Gateway = 62;
    public const uint Forge = 63;
    public const uint FleetBeacon = 64;
    public const uint TwilightCouncil = 65;
    public const uint PhotonCannon = 66;
    public const uint ShieldBattery = 1910;
    public const uint Stargate = 67;
    public const uint TemplarArchive = 68;
    public const uint DarkShrine = 69;
    public const uint RoboticsBay = 70;
    public const uint RoboticsFacility = 71;
    public const uint CyberneticsCore = 72;
    public const uint Zealot = 73;
    public const uint Stalker = 74;
    public const uint Adept = 311;
    public const uint HighTemplar = 75;
    public const uint DarkTemplar = 76;
    public const uint Sentry = 77;
    public const uint Phoenix = 78;
    public const uint Carrier = 79;
    public const uint VoidRay = 80;
    public const uint WarpPrism = 81;
    public const uint Observer = 82;
    public const uint Immortal = 83;
    public const uint Probe = 84;
    public const uint Interceptor = 85;
    public const uint Hatchery = 86;
    public const uint CreepTumor = 87;
    public const uint Extractor = 88;
    public const uint SpawningPool = 89;
    public const uint EvolutionChamber = 90;
    public const uint HydraliskDen = 91;
    public const uint Spire = 92;
    public const uint UltraliskCavern = 93;
    public const uint InfestationPit = 94;
    public const uint NydusNetwork = 95;
    public const uint BanelingNest = 96;
    public const uint RoachWarren = 97;
    public const uint SpineCrawler = 98;
    public const uint SporeCrawler = 99;
    public const uint Lair = 100;
    public const uint Hive = 101;
    public const uint GreaterSpire = 102;
    public const uint Egg = 103;
    public const uint Drone = 104;
    public const uint Zergling = 105;
    public const uint Overlord = 106;
    public const uint OverlordTransport = 893;
    public const uint Hydralisk = 107;
    public const uint Mutalisk = 108;
    public const uint Ultralisk = 109;
    public const uint Roach = 110;
    public const uint Infestor = 111;
    public const uint Corruptor = 112;
    public const uint BroodLordCocoon = 113;
    public const uint BroodLord = 114;
    public const uint Broodling = 289; // TODO GD Which one is it?
    public const uint BroodlingEscort = 143; // TODO GD Which one is it?
    public const uint BanelingBurrowed = 115;
    public const uint DroneBurrowed = 116;
    public const uint HydraliskBurrowed = 117;
    public const uint RoachBurrowed = 118;
    public const uint ZerglingBurrowed = 119;
    public const uint InfestorTerranBurrowed = 120;
    public const uint QueenBurrowed = 125;
    public const uint Queen = 126;
    public const uint InfestorBurrowed = 127;
    public const uint OverlordCocoon = 128;
    public const uint Overseer = 129;
    public const uint PlanetaryFortress = 130;
    public const uint UltraliskBurrowed = 131;
    public const uint OrbitalCommand = 132;
    public const uint WarpGate = 133;
    public const uint OrbitalCommandFlying = 134;
    public const uint ForceField = 135;
    public const uint WarpPrismPhasing = 136;
    public const uint CreepTumorBurrowed = 137;
    public const uint CreepTumorQueen = 138;
    public const uint SpineCrawlerUprooted = 139;
    public const uint SporeCrawlerUprooted = 140;
    public const uint Archon = 141;
    public const uint NydusWorm = 142;
    public const uint RichMineralField = 146;
    public const uint RichMineralField750 = 147;
    public const uint Ursadon = 148;
    public const uint XelNagaTower = 149;
    public const uint InfestedTerransEgg = 150;
    public const uint Larva = 151;
    public const uint MineralField = 341;
    public const uint VespeneGeyser = 342;
    public const uint SpacePlatformGeyser = 343;
    public const uint RichVespeneGeyser = 344;
    public const uint MineralField750 = 483;
    public const uint ProtossVespeneGeyser = 608;
    public const uint LabMineralField = 665;
    public const uint LabMineralField750 = 666;
    public const uint PurifierRichMineralField = 796;
    public const uint PurifierRichMineralField750 = 797;
    public const uint PurifierVespeneGeyser = 880;
    public const uint ShakurasVespeneGeyser = 881;
    public const uint PurifierMineralField = 884;
    public const uint PurifierMineralField750 = 885;
    public const uint BattleStationMineralField = 886;
    public const uint BattleStationMineralField750 = 887;

    /// <summary>
    /// Represents tiny mineral patches (10 or so minerals) that can be cleared to gain access to expand locations or ramps.
    /// They are not used as mineral patches to mine.
    /// </summary>
    public const uint MineralField450  = 1996;

    public const uint SwarmHost = 494; // SwarmHostMP?
    public const uint SwarmHostBurrowed = 493; // SwarmHostBurrowedMP?
    public const uint Viper = 499;
    public const uint Lurker = 502; // LurkerMP?
    public const uint LurkerBurrowed = 503; // LurkerMPBurrowed?
    public const uint LurkerDen = 504; // LurkerDenMP?
    public const uint Ravager = 688;
    public const uint RavagerBurrowed = 690;

    public const uint Disruptor = 694;
    public const uint Oracle = 495;
    public const uint Tempest = 496;

    public const uint DestructibleSearchlight = 345;
    public const uint DestructibleBullhornLights = 346;
    public const uint DestructibleStreetlight = 347;
    public const uint DestructibleSpacePlatformSign = 348;
    public const uint DestructibleStoreFrontCityProps = 349;
    public const uint DestructibleBillboardTall = 350;
    public const uint DestructibleBillboardScrollingText = 351;
    public const uint DestructibleSpacePlatformBarrier = 352;
    public const uint DestructibleSignsDirectional = 353;
    public const uint DestructibleSignsConstruction = 354;
    public const uint DestructibleSignsFunny = 355;
    public const uint DestructibleSignsIcons = 356;
    public const uint DestructibleSignsWarning = 357;
    public const uint DestructibleGarage = 358;
    public const uint DestructibleGarageLarge = 359;
    public const uint DestructibleTrafficSignal = 360;
    public const uint BraxisAlphaDestructible1x1 = 362;
    public const uint BraxisAlphaDestructible2x2 = 363;
    public const uint DestructibleDebris4x4 = 364;
    public const uint DestructibleDebris6x6 = 365;
    public const uint DestructibleRock2x4Vertical = 366;
    public const uint DestructibleRock2x4Horizontal = 367;
    public const uint DestructibleRock2x6Vertical = 368;
    public const uint DestructibleRock2x6Horizontal = 369;
    public const uint DestructibleRock4x4 = 370;
    public const uint DestructibleRock6x6 = 371;
    public const uint DestructibleRampDiagonalHugeULBR = 372;
    public const uint DestructibleRampDiagonalHugeBLUR = 373;
    public const uint DestructibleRampVerticalHuge = 374;
    public const uint DestructibleRampHorizontalHuge = 375;
    public const uint DestructibleDebrisRampDiagonalHugeULBR = 376;
    public const uint DestructibleDebrisRampDiagonalHugeBLUR = 377;
    public const uint DestructibleRockEx16x6 = 639;
    public const uint DestructibleRockEx1DiagonalHugeULBR = 640;
    public const uint DestructibleRockEx14x4 = 638;
    public const uint DestructibleRockEx1HorizontalHuge = 643;
    public const uint DestructibleCityDebris6x6 = 629;
    public const uint DestructibleCityDebrisHugeDiagonalBLUR = 630;
    public const uint CollapsibleTerranTowerDiagonal = 589;
    public const uint CollapsibleRockTowerDiagonal = 588;
    public const uint DestructibleRockEx1DiagonalHugeBLUR = 641;
    public const uint CollapsiblePurifierTowerDiagonal = 882;

    public const uint UnbuildableBricksDestructible = 473;
    public const uint UnbuildablePlatesDestructible = 474;
    public const uint UnbuildableRocksDestructible = 472;
    public const uint Rocks2x2NonConjoined = 312;

    public const uint ReaperMine = 831; // KD8Charge

    #endregion

    public static readonly HashSet<uint> Workers = new HashSet<uint>
    {
        Scv,
        Probe,
        Drone,
    };

    public static readonly HashSet<uint> MobileDetectors = new HashSet<uint>
    {
        Overseer,
        Observer,
        Raven,
    };

    public static readonly HashSet<uint> StaticDetectors = new HashSet<uint>
    {
        SporeCrawler,
        PhotonCannon,
        MissileTurret,
    };

    public static readonly HashSet<uint> Detectors = new HashSet<uint>(MobileDetectors.Concat(StaticDetectors));

    public static readonly HashSet<uint> DropShips = new HashSet<uint>
    {
        Medivac,
        WarpPrism,
        WarpPrismPhasing,
        OverlordTransport,
    };

    // TODO GD Might be able to use the unitTypeData.alias for this?
    public static readonly Dictionary<uint, HashSet<uint>> EquivalentTo = new Dictionary<uint, HashSet<uint>>
    {
        { Hatchery,      new HashSet<uint> { Lair, Hive } },
        { Lair,          new HashSet<uint> { Hive } },
        { Spire,         new HashSet<uint> { GreaterSpire } },
        { CommandCenter, new HashSet<uint> { OrbitalCommand, PlanetaryFortress } },
        { CreepTumor,    new HashSet<uint> { CreepTumorQueen, CreepTumorBurrowed } },
        { Roach,         new HashSet<uint> { RoachBurrowed } }, // TODO GD Do all the burrowed units, or something nicer
    };

    #region Terran

    public static readonly HashSet<uint> TerranTownHalls = new HashSet<uint>
    {
        CommandCenter,
        CommandCenterFlying,
        OrbitalCommand,
        OrbitalCommandFlying,
        PlanetaryFortress,
    };

    public static readonly HashSet<uint> TerranExtractor = new HashSet<uint>
    {
        Refinery,
    };

    public static readonly HashSet<uint> TerranSupplyBuildings = new HashSet<uint>
    {
        SupplyDepot,
        SupplyDepotLowered,
    };

    public static readonly HashSet<uint> TerranTechBuildings = new HashSet<uint>
    {
        EngineeringBay,
        Armory,
        FusionCore,
    };

    public static readonly HashSet<uint> TerranDefenseBuildings = new HashSet<uint>
    {
        MissileTurret,
        Bunker,
        SensorTower,
    };

    public static readonly HashSet<uint> TerranProductionBuildings = new HashSet<uint>
    {
        Barracks,
        BarracksFlying,
        Factory,
        FactoryFlying,
        Starport,
        StarportFlying,
        GhostAcademy,
    };

    public static readonly HashSet<uint> TerranAddOnBuildings = new HashSet<uint>
    {
        BarracksReactor,
        BarracksTechlab,
        FactoryReactor,
        FactoryTechlab,
        StarportReactor,
        StarportTechlab,
    };

    public static readonly HashSet<uint> TerranBuildings = new HashSet<uint>(
        TerranTownHalls
            .Concat(TerranExtractor)
            .Concat(TerranSupplyBuildings)
            .Concat(TerranTechBuildings)
            .Concat(TerranDefenseBuildings)
            .Concat(TerranProductionBuildings)
            .Concat(TerranAddOnBuildings)
    );

    public static readonly HashSet<uint> TerranMilitary = new HashSet<uint>
    {
        Marine,
        Reaper,
        Marauder,
        Ghost,
        Hellion,
        WidowMine,
        WidowMineBurrowed,
        Cyclone,
        SiegeTank,
        SiegeTankSieged,
        Thor,
        Hellbat,

        // Flying
        VikingAssault,
        VikingFighter,
        Medivac,
        Liberator,
        Raven,
        AutoTurret,
        Banshee,
        Battlecruiser,
    };

    public static readonly HashSet<uint> AllTerranUnits = new HashSet<uint>(
        TerranBuildings
            .Concat(TerranMilitary)
            .Concat(new [] { Scv })
    );

    #endregion

    #region Protoss

    public static readonly HashSet<uint> ProtossTownHalls = new HashSet<uint>
    {
        Nexus,
    };

    public static readonly HashSet<uint> ProtossExtractor = new HashSet<uint>
    {
        Assimilator,
    };

    public static readonly HashSet<uint> ProtossSupplyBuildings = new HashSet<uint>
    {
        Pylon,
    };

    public static readonly HashSet<uint> ProtossTechBuildings = new HashSet<uint>
    {
        Forge,
        CyberneticsCore,
        TwilightCouncil,
        TemplarArchive,
        DarkShrine,
        FleetBeacon,
    };

    public static readonly HashSet<uint> ProtossDefenseBuildings = new HashSet<uint>
    {
        PhotonCannon,
        ShieldBattery,
    };

    public static readonly HashSet<uint> ProtossProductionBuildings = new HashSet<uint>
    {
        Gateway,
        WarpGate,
        Stargate,
        RoboticsFacility,
        RoboticsBay,
    };

    public static readonly HashSet<uint> ProtossBuildings = new HashSet<uint>(
        ProtossTownHalls
            .Concat(ProtossExtractor)
            .Concat(ProtossSupplyBuildings)
            .Concat(ProtossTechBuildings)
            .Concat(ProtossDefenseBuildings)
            .Concat(ProtossProductionBuildings)
    );

    public static readonly HashSet<uint> ProtossMilitary = new HashSet<uint>
    {
        Zealot,
        Sentry,
        Stalker,
        Adept,
        Immortal,
        HighTemplar,
        Archon,
        DarkTemplar,
        Colossus,
        Disruptor,

        // Flying
        Phoenix,
        Oracle,
        VoidRay,
        WarpPrism,
        WarpPrismPhasing,
        Tempest,
        Carrier,
        Mothership,
    };

    public static readonly HashSet<uint> AllProtossUnits = new HashSet<uint>(
        ProtossBuildings
            .Concat(ProtossMilitary)
            .Concat(new [] { Probe })
    );

    #endregion

    #region Zerg

    public static readonly HashSet<uint> ZergTownHalls = new HashSet<uint>
    {
        Hatchery,
        Lair,
        Hive,
    };

    public static readonly HashSet<uint> ZergExtractor = new HashSet<uint>
    {
        Extractor,
    };

    public static readonly HashSet<uint> ZergSupplyBuildings = new HashSet<uint>
    {
        Overlord,
        Overseer,
        OverlordTransport,
    };

    public static readonly HashSet<uint> ZergTechBuildings = new HashSet<uint>
    {
        EvolutionChamber,
        Lair,
        Hive,
    };

    public static readonly HashSet<uint> ZergDefenseBuildings = new HashSet<uint>
    {
        SpineCrawler,
        SpineCrawlerUprooted,
        SporeCrawler,
        SporeCrawlerUprooted,

        // Maybe this doesn't go here but for now I don't know any better
        NydusNetwork,
        NydusWorm,
    };

    public static readonly HashSet<uint> ZergProductionBuildings = new HashSet<uint>
    {
        SpawningPool,
        RoachWarren,
        BanelingNest,
        HydraliskDen,
        LurkerDen,
        InfestationPit,
        Spire,
        GreaterSpire,
        UltraliskCavern,
    };

    public static readonly HashSet<uint> ZergBuildings = new HashSet<uint>(
        ZergTownHalls
            .Concat(ZergExtractor)
            .Concat(ZergSupplyBuildings)
            .Concat(ZergTechBuildings)
            .Concat(ZergDefenseBuildings)
            .Concat(ZergProductionBuildings)
    );

    public static readonly HashSet<uint> ZergMilitary = new HashSet<uint>
    {
        // Ground
        Queen,
        QueenBurrowed,
        Zergling,
        ZerglingBurrowed,
        Baneling,
        BanelingBurrowed,
        Roach,
        RoachBurrowed,
        Ravager,
        RavagerBurrowed,
        Hydralisk,
        HydraliskBurrowed,
        Lurker,
        LurkerBurrowed,
        SwarmHost,
        SwarmHostBurrowed,
        Infestor,
        InfestorBurrowed,
        InfestorTerran,
        InfestorTerranBurrowed,
        Ultralisk,
        UltraliskBurrowed,

        // Flying
        Mutalisk,
        Corruptor,
        BroodLord,
        Viper,
    };

    public static readonly HashSet<uint> CreepTumors = new HashSet<uint>
    {
        CreepTumor,
        CreepTumorBurrowed,
        CreepTumorQueen,
    };

    public static readonly HashSet<uint> Changelings = new HashSet<uint>
    {
        Changeling,
        ChangelingZealot,
        ChangelingMarineShield,
        ChangelingMarine,
        ChangelingZerglingWings,
        ChangelingZergling,
    };

    public static readonly HashSet<uint> AllZergUnits = new HashSet<uint>(
        ZergBuildings
            .Concat(ZergMilitary)
            .Concat(CreepTumors)
            .Concat(Changelings)
            .Concat(new [] { Drone })
    );

    #endregion

    #region RaceAgnostic

    public static readonly HashSet<uint> Buildings = new HashSet<uint>(
        TerranBuildings
            .Concat(ProtossBuildings)
            .Concat(ZergBuildings)
    );

    public static readonly HashSet<uint> Military = new HashSet<uint>(
        TerranMilitary
            .Concat(ProtossMilitary)
            .Concat(ZergMilitary)
    );

    public static readonly HashSet<uint> TownHalls = new HashSet<uint>(
        TerranTownHalls
            .Concat(ProtossTownHalls)
            .Concat(ZergTownHalls)
    );

    public static readonly HashSet<uint> StaticDefenses = new HashSet<uint>(
        TerranDefenseBuildings
            .Concat(ProtossDefenseBuildings)
            .Concat(ZergDefenseBuildings)
    );

    public static readonly HashSet<uint> Extractors = new HashSet<uint>(
        TerranExtractor
            .Concat(ProtossExtractor)
            .Concat(ZergExtractor)
    );

    public static readonly HashSet<uint> TechBuildings = new HashSet<uint>(
        TerranTechBuildings
            .Concat(ProtossTechBuildings)
            .Concat(ZergTechBuildings)
    );

    public static readonly HashSet<uint> ProductionBuildings = new HashSet<uint>(
        TerranProductionBuildings
            .Concat(ProtossProductionBuildings)
            .Concat(ZergProductionBuildings)
    );

    public static readonly HashSet<uint> SupplyBuildings = new HashSet<uint>(
        TerranSupplyBuildings
            .Concat(ProtossSupplyBuildings)
            .Concat(ZergSupplyBuildings)
    );

    #endregion

    #region Rocks

    public static readonly HashSet<uint> BuildBlockers = new HashSet<uint>
    {
        UnbuildablePlatesDestructible,
        UnbuildableRocksDestructible,
        UnbuildableBricksDestructible,
        Rocks2x2NonConjoined,
    };

    public static readonly HashSet<uint> Obstacles = new HashSet<uint>
    {
        DestructibleSearchlight,
        DestructibleBullhornLights,
        DestructibleStreetlight,
        DestructibleSpacePlatformSign,
        DestructibleStoreFrontCityProps,
        DestructibleBillboardTall,
        DestructibleBillboardScrollingText,
        DestructibleSpacePlatformBarrier,
        DestructibleSignsDirectional,
        DestructibleSignsConstruction,
        DestructibleSignsFunny,
        DestructibleSignsIcons,
        DestructibleSignsWarning,
        DestructibleGarage,
        DestructibleGarageLarge,
        DestructibleTrafficSignal,
        BraxisAlphaDestructible1x1,
        BraxisAlphaDestructible2x2,
        DestructibleDebris4x4,
        DestructibleDebris6x6,
        DestructibleRock2x4Vertical,
        DestructibleRock2x4Horizontal,
        DestructibleRock2x6Vertical,
        DestructibleRock2x6Horizontal,
        DestructibleRock4x4,
        DestructibleRock6x6,
        DestructibleRampDiagonalHugeULBR,
        DestructibleRampDiagonalHugeBLUR,
        DestructibleRampVerticalHuge,
        DestructibleRampHorizontalHuge,
        DestructibleDebrisRampDiagonalHugeULBR,
        DestructibleDebrisRampDiagonalHugeBLUR,
        DestructibleRockEx16x6,
        DestructibleRockEx1DiagonalHugeULBR,
        DestructibleRockEx14x4,
        DestructibleRockEx1HorizontalHuge,
        DestructibleCityDebris6x6,
        DestructibleCityDebrisHugeDiagonalBLUR,
        CollapsibleTerranTowerDiagonal,
        CollapsibleRockTowerDiagonal,
        DestructibleRockEx1DiagonalHugeBLUR,
        CollapsiblePurifierTowerDiagonal,
    };

    public static readonly HashSet<uint> Destructibles = new HashSet<uint>(
        BuildBlockers
            .Concat(Obstacles)
    );

    #endregion

    #region Resources

    // Mineral field types seem to differ from map to map
    public static readonly HashSet<uint> BlueMineralFields = new HashSet<uint>
    {
        MineralField,
        MineralField750,
        LabMineralField,
        LabMineralField750,
        PurifierMineralField,
        PurifierMineralField750,
        BattleStationMineralField,
        BattleStationMineralField750,
        MineralField450, // Should we exclude this one? See note
    };

    public static readonly HashSet<uint> GoldMineralFields = new HashSet<uint>
    {
        RichMineralField,
        RichMineralField750,
        PurifierRichMineralField,
        PurifierRichMineralField750,
    };

    public static readonly HashSet<uint> MineralFields = new HashSet<uint>(BlueMineralFields.Concat(GoldMineralFields));

    // Gas geyser types seem to differ from map to map
    public static readonly HashSet<uint> GreenGasGeysers = new HashSet<uint>
    {
        VespeneGeyser,
        SpacePlatformGeyser,
        RichVespeneGeyser,
        ProtossVespeneGeyser,
        PurifierVespeneGeyser,
        ShakurasVespeneGeyser,
    };

    public static readonly HashSet<uint> PurpleGasGeysers = new HashSet<uint>
    {
        RichVespeneGeyser,
    };

    public static readonly HashSet<uint> GasGeysers = new HashSet<uint>(GreenGasGeysers.Concat(PurpleGasGeysers));

    #endregion
}
