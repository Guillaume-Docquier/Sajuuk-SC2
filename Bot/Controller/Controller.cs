using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.GameData;
using Bot.UnitModules;
using Bot.Wrapper;
using SC2APIProtocol;
using Action = SC2APIProtocol.Action;

namespace Bot;

public static class Controller {
    private const int FrameDelay = 0; // Too fast? increase this to e.g. 20

    private static readonly List<Action> Actions = new List<Action>();
    private static readonly Random Random = new Random();
    public const double FramesPerSecond = 22.4;
    public const float ExpandIsTakenRadius = 4f;

    private static readonly UnitsTracker UnitsTracker = new UnitsTracker();
    private static readonly BuildingTracker BuildingTracker = new BuildingTracker();

    public static ResponseGameInfo GameInfo;
    private static ResponseObservation _obs;

    public static ulong Frame = ulong.MaxValue;

    public static uint CurrentSupply;
    public static uint MaxSupply;
    public static int AvailableSupply => (int)(MaxSupply - CurrentSupply);

    public static bool IsSupplyCapped => AvailableSupply == 0;

    public static int AvailableMinerals;
    public static int AvailableVespene;
    public static HashSet<uint> ResearchedUpgrades;

    public static Unit StartingTownHall;
    public static readonly List<Vector3> EnemyLocations = new List<Vector3>();
    public static readonly List<string> ChatLog = new List<string>();
    public static Dictionary<ulong, Unit> UnitsByTag => UnitsTracker.UnitsByTag;
    public static List<Unit> OwnedUnits => UnitsTracker.OwnedUnits;
    public static List<Unit> NewOwnedUnits => UnitsTracker.NewOwnedUnits;
    public static List<Unit> DeadOwnedUnits => UnitsTracker.DeadOwnedUnits;
    public static List<Unit> NeutralUnits => UnitsTracker.NeutralUnits;
    public static List<Unit> EnemyUnits => UnitsTracker.EnemyUnits;

    public static void Pause() {
        Console.WriteLine("Press any key to continue...");
        while (Console.ReadKey().Key != ConsoleKey.Enter) {
            //do nothing
        }
    }

    public static ulong SecsToFrames(int seconds) {
        return (ulong)(FramesPerSecond * seconds);
    }

    public static void NewObservation(ResponseObservation obs) {
        _obs = obs;
        Frame = _obs.Observation.GameLoop;

        if (GameInfo == null || KnowledgeBase.Data == null || _obs == null) {
            if (GameInfo == null) {
                Logger.Info("GameInfo is null! The application will terminate.");
            }
            else if (KnowledgeBase.Data == null) {
                Logger.Info("TypeData is null! The application will terminate.");
            }
            else {
                Logger.Info("ResponseObservation is null! The application will terminate.");
            }

            Pause();
            Environment.Exit(0);
        }

        UnitsTracker.Update(_obs.Observation.RawData.Units.ToList(), Frame);
        BuildingTracker.Update();

        Actions.Clear();

        foreach (var chat in _obs.Chat) {
            ChatLog.Add(chat.Message);
        }

        CurrentSupply = _obs.Observation.PlayerCommon.FoodUsed;
        var hasOddAmountOfZerglings = OwnedUnits.Count(unit => unit.UnitType == Units.Zergling) % 2 == 1;
        if (hasOddAmountOfZerglings) {
            // Zerglings have 0.5 supply. The api returns a rounded down supply, but the game considers the rounded up supply.
            CurrentSupply += 1;
        }

        MaxSupply = _obs.Observation.PlayerCommon.FoodCap;

        AvailableMinerals = _obs.Observation.PlayerCommon.Minerals;
        AvailableVespene = _obs.Observation.PlayerCommon.Vespene;
        ResearchedUpgrades = new HashSet<uint>(_obs.Observation.RawData.Player.UpgradeIds);

        if (Frame == 0) {
            var townHalls = GetUnits(OwnedUnits, Units.ResourceCenters).ToList();
            if (townHalls.Count > 0) {
                StartingTownHall = townHalls[0];

                foreach (var startLocation in GameInfo.StartRaw.StartLocations) {
                    var enemyLocation = new Vector3(startLocation.X, startLocation.Y, 0);
                    if (StartingTownHall.DistanceTo(enemyLocation) > 30) {
                        EnemyLocations.Add(enemyLocation);
                    }
                }
            }
        }
    }

    public static IEnumerable<Action> GetActions() {
        return Actions;
    }

    public static void AddAction(Action action) {
        Actions.Add(action);
    }

    public static void Chat(string message, bool toTeam = false) {
        AddAction(ActionBuilder.Chat(message, toTeam));
    }

    private static int GetTotalCount(uint unitType) {
        var pendingCount = GetPendingCount(unitType, inConstruction: false);
        var constructionCount = GetUnits(OwnedUnits, unitType).Count();

        return pendingCount + constructionCount;
    }

    private static int GetPendingCount(uint unitType, bool inConstruction = true) {
        var workers = GetUnits(OwnedUnits, Units.Workers);
        var abilityId = KnowledgeBase.GetUnitTypeData(unitType).AbilityId;

        var counter = 0;

        // Count workers that have been sent to build this structure
        foreach (var worker in workers) {
            if (worker.Orders.Any(order => order.AbilityId == abilityId)) {
                counter += 1;
            }
        }

        // Count buildings that are already in construction
        if (inConstruction) {
            foreach (var unit in GetUnits(OwnedUnits, unitType)) {
                if (!unit.IsOperational) {
                    counter += 1;
                }
            }
        }

        return counter;
    }

    public static Unit GetAvailableProducer(uint unitOrAbilityType, bool allowQueue = false) {
        if (!Units.Producers.ContainsKey(unitOrAbilityType)) {
            throw new NotImplementedException($"Producer for unit {KnowledgeBase.GetUnitTypeData(unitOrAbilityType).Name} not found");
        }

        var possibleProducers = Units.Producers[unitOrAbilityType];

        var producers = GetUnits(OwnedUnits, possibleProducers, onlyCompleted: true)
            .Where(unit => unit.IsAvailable)
            .OrderBy(unit => unit.OrdersExceptMining.Count());

        if (!allowQueue) {
            return producers.FirstOrDefault(unit => !unit.OrdersExceptMining.Any());
        }

        return producers.FirstOrDefault();
    }

    public static bool ExecuteBuildStep(BuildOrders.BuildStep buildStep) {
        return buildStep.BuildType switch
        {
            BuildType.Train => TrainUnit(buildStep.UnitOrUpgradeType),
            BuildType.Build => PlaceBuilding(buildStep.UnitOrUpgradeType),
            BuildType.Research => ResearchUpgrade(buildStep.UnitOrUpgradeType),
            BuildType.UpgradeInto => UpgradeInto(buildStep.UnitOrUpgradeType),
            BuildType.Expand => PlaceExpand(buildStep.UnitOrUpgradeType),
            _ => false
        };
    }

    public static bool TrainUnit(uint unitType) {
        var producer = GetAvailableProducer(unitType);

        return TrainUnit(unitType, producer);
    }

    public static bool TrainUnit(uint unitType, Unit producer)
    {
        var unitTypeData = KnowledgeBase.GetUnitTypeData(unitType);
        if (producer == null || !CanAfford(unitTypeData) || !HasEnoughSupply(unitType) || !IsUnlocked(unitType)) {
            return false;
        }

        producer.TrainUnit(unitType);

        AvailableMinerals -= unitTypeData.MineralCost;
        AvailableVespene -= unitTypeData.VespeneCost;
        CurrentSupply += Convert.ToUInt32(Math.Ceiling(unitTypeData.FoodRequired)); // Round up zerglings food, will be corrected on next frame

        return true;
    }

    public static bool PlaceBuilding(uint buildingType, Vector3 location = default) {
        var producer = GetAvailableProducer(buildingType);

        return PlaceBuilding(buildingType, producer, location);
    }

    public static bool PlaceBuilding(uint buildingType, Unit producer, Vector3 location = default) {
        var buildingTypeData = KnowledgeBase.GetUnitTypeData(buildingType);
        if (producer == null || !CanAfford(buildingTypeData) || !IsUnlocked(buildingType)) {
            return false;
        }

        if (buildingType == Units.Extractor) {
            // TODO GD Prioritize the main base, get a nearby worker
            var availableGas = GetUnits(NeutralUnits, Units.GasGeysers, onlyVisible: true)
                .FirstOrDefault(gas => UnitUtils.IsResourceManaged(gas) && !UnitUtils.IsGasExploited(gas));

            if (availableGas == null) {
                return false;
            }

            producer.PlaceExtractor(buildingType, availableGas);
            UnitModule.Get<CapacityModule>(availableGas).Assign(producer); // Assign the worker until extractor is spawned
            BuildingTracker.ConfirmPlacement(buildingType, availableGas.Position, producer);
        }
        else if (location != default) {
            if (!BuildingTracker.CanPlace(buildingType, location)) {
                return false;
            }

            producer.PlaceBuilding(buildingType, location);
            BuildingTracker.ConfirmPlacement(buildingType, location, producer);
        }
        else {
            var constructionSpot = BuildingTracker.FindConstructionSpot(buildingType);

            producer.PlaceBuilding(buildingType, constructionSpot);
            BuildingTracker.ConfirmPlacement(buildingType, constructionSpot, producer);
        }

        AvailableMinerals -= buildingTypeData.MineralCost;
        AvailableVespene -= buildingTypeData.VespeneCost;

        return true;
    }

    public static bool CanPlace(uint unitType, Vector3 targetPos) {
        return BuildingTracker.CanPlace(unitType, targetPos);
    }

    public static bool ResearchUpgrade(uint upgradeType) {
        var producer = GetAvailableProducer(upgradeType, allowQueue: true);

        return ResearchUpgrade(upgradeType, producer);
    }

    public static bool ResearchUpgrade(uint upgradeType, Unit producer) {
        var researchTypeData = KnowledgeBase.GetUpgradeData(upgradeType);
        if (producer == null || !CanAfford(researchTypeData) || !IsUnlocked(upgradeType)) {
            return false;
        }

        producer.ResearchUpgrade(upgradeType);

        AvailableMinerals -= researchTypeData.MineralCost;
        AvailableVespene -= researchTypeData.VespeneCost;

        return true;
    }

    public static bool UpgradeInto(uint buildingType) {
        var producer = GetAvailableProducer(buildingType);

        return UpgradeInto(buildingType, producer);
    }

    public static bool UpgradeInto(uint buildingType, Unit producer) {
        var buildingTypeData = KnowledgeBase.GetUnitTypeData(buildingType);
        if (producer == null || !CanAfford(buildingTypeData) || !IsUnlocked(buildingType)) {
            return false;
        }

        producer.UpgradeInto(buildingType);

        AvailableMinerals -= buildingTypeData.MineralCost;
        AvailableVespene -= buildingTypeData.VespeneCost;

        return true;
    }

    public static bool PlaceExpand(uint buildingType) {
        if (!MapAnalyzer.IsInitialized) {
            return false;
        }

        var expandLocation = MapAnalyzer.ExpandLocations
            .Where(expandLocation => !GetUnits(OwnedUnits, Units.Hatchery).Any(townHall => townHall.DistanceTo(expandLocation) < ExpandIsTakenRadius)) // Ignore expands that are taken
            .OrderBy(expandLocation => StartingTownHall.DistanceTo(expandLocation))
            .Take(3) // TODO GD Keep going if you don't find in the first 3
            .OrderBy(expandLocation => Pathfinder.FindPath(StartingTownHall.Position, expandLocation).Count)
            .First(expandLocation => BuildingTracker.CanPlace(buildingType, expandLocation));

        return PlaceBuilding(buildingType, expandLocation);
    }

    // TODO GD Zerg Specific
    public static IEnumerable<Unit> GetUnitsInProduction(uint unitType) {
        var unitToGetAbilityId =  KnowledgeBase.GetUnitTypeData(unitType).AbilityId;

        return GetUnits(OwnedUnits, Units.Egg).Where(egg => egg.Orders.Any(order => order.AbilityId == unitToGetAbilityId));
    }

    public static IEnumerable<Unit> GetUnits(IEnumerable<Unit> unitPool, uint unitToGet, bool onlyCompleted = false, bool onlyVisible = false) {
        return GetUnits(unitPool, new HashSet<uint>{ unitToGet }, onlyCompleted, onlyVisible);
    }

    public static IEnumerable<Unit> GetUnits(IEnumerable<Unit> unitPool, HashSet<uint> unitsToGet, bool onlyCompleted = false, bool onlyVisible = false) {
        var equivalentUnits = unitsToGet
            .Where(unitToGet => Units.EquivalentTo.ContainsKey(unitToGet))
            .SelectMany(unitToGet => Units.EquivalentTo[unitToGet])
            .ToList();

        unitsToGet.UnionWith(equivalentUnits);

        foreach (var unit in unitPool) {
            if (unitsToGet.Contains(unit.UnitType)) {
                if (onlyCompleted && !unit.IsOperational) {
                    continue;
                }

                if (onlyVisible && !unit.IsVisible) {
                    continue;
                }

                yield return unit;
            }
        }
    }

    public static bool CanAfford(UpgradeData upgradeData)
    {
        return CanAfford(upgradeData.MineralCost, upgradeData.VespeneCost);
    }

    public static bool CanAfford(UnitTypeData unitTypeData) {
        return CanAfford(unitTypeData.MineralCost, unitTypeData.VespeneCost);
    }

    public static bool CanAfford(int mineralCost, int vespeneCost)
    {
        return AvailableMinerals >= mineralCost && AvailableVespene >= vespeneCost;
    }

    public static bool IsUnlocked(uint unitType) {
        if (Units.Prerequisites.TryGetValue(unitType, out var prerequisiteUnitType)) {
            return GetUnits(OwnedUnits, prerequisiteUnitType, onlyCompleted: true).Any();
        }

        return true;
    }

    public static bool HasEnoughSupply(uint unitType) {
        return AvailableSupply >= KnowledgeBase.GetUnitTypeData(unitType).FoodRequired;
    }

    public static IEnumerable<Unit> GetMiningTownHalls() {
        return GetUnits(OwnedUnits, Units.Hatchery)
            .Where(townHall => MapAnalyzer.ExpandLocations.Any(expandLocation => townHall.DistanceTo(expandLocation) < ExpandIsTakenRadius));
    }
}
