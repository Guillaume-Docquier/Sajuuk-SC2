using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using Bot.GameData;
using Bot.UnitModules;
using Bot.Wrapper;
using SC2APIProtocol;
using Action = SC2APIProtocol.Action;

namespace Bot;

public static class Controller {
    public const int RealTime = (int)(1000 / FramesPerSecond);
    public static int FrameDelayMs = 0; // Too fast? increase this to e.g. 20

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

        // This doesn't work? I don't really care but it would be nice if it did
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
        if (FrameDelayMs > 0) {
            Thread.Sleep(FrameDelayMs);
        }

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

    public static RequestResult ExecuteBuildStep(BuildOrders.BuildStep buildStep) {
        return buildStep.BuildType switch
        {
            BuildType.Train => TrainUnit(buildStep.UnitOrUpgradeType),
            BuildType.Build => PlaceBuilding(buildStep.UnitOrUpgradeType),
            BuildType.Research => ResearchUpgrade(buildStep.UnitOrUpgradeType),
            BuildType.UpgradeInto => UpgradeInto(buildStep.UnitOrUpgradeType),
            BuildType.Expand => PlaceExpand(buildStep.UnitOrUpgradeType),
            _ => RequestResult.NotSupported
        };
    }

    private static RequestResult ValidateRequirements(uint unitType, Unit producer, UnitTypeData unitTypeData) {
        return ValidateRequirements(unitType, producer, unitTypeData.MineralCost, unitTypeData.VespeneCost, unitTypeData.FoodRequired);
    }

    private static RequestResult ValidateRequirements(uint upgradeType, Unit producer, UpgradeData upgradeData) {
        return ValidateRequirements(upgradeType, producer, upgradeData.MineralCost, upgradeData.VespeneCost);
    }

    private static RequestResult ValidateRequirements(uint unitType, Unit producer, int mineralCost, int vespeneCost, float foodCost = 0) {
        if (producer == null) {
            return RequestResult.NoProducersAvailable;
        }

        var canAffordResult = CanAfford(mineralCost, vespeneCost);
        if (canAffordResult != RequestResult.Ok) {
            return canAffordResult;
        }

        if (!HasEnoughSupply(foodCost)) {
            return RequestResult.NotEnoughSupply;
        }

        if (!IsUnlocked(unitType)) {
            return RequestResult.TechRequirementsNotMet;
        }

        return RequestResult.Ok;
    }

    public static RequestResult TrainUnit(uint unitType) {
        var producer = GetAvailableProducer(unitType);

        return TrainUnit(unitType, producer);
    }

    public static RequestResult TrainUnit(uint unitType, Unit producer) {
        var unitTypeData = KnowledgeBase.GetUnitTypeData(unitType);

        var requirementsValidationResult = ValidateRequirements(unitType, producer, unitTypeData);
        if (requirementsValidationResult != RequestResult.Ok) {
            return requirementsValidationResult;
        }

        producer.TrainUnit(unitType);

        AvailableMinerals -= unitTypeData.MineralCost;
        AvailableVespene -= unitTypeData.VespeneCost;
        CurrentSupply += Convert.ToUInt32(Math.Ceiling(unitTypeData.FoodRequired)); // Round up zerglings food, will be corrected on next frame

        return RequestResult.Ok;
    }

    public static RequestResult PlaceBuilding(uint buildingType, Vector3 location = default) {
        var producer = GetAvailableProducer(buildingType);

        return PlaceBuilding(buildingType, producer, location);
    }

    public static RequestResult PlaceBuilding(uint buildingType, Unit producer, Vector3 location = default) {
        var buildingTypeData = KnowledgeBase.GetUnitTypeData(buildingType);

        var requirementsValidationResult = ValidateRequirements(buildingType, producer, buildingTypeData);
        if (requirementsValidationResult != RequestResult.Ok) {
            return requirementsValidationResult;
        }

        if (buildingType == Units.Extractor) {
            // TODO GD Prioritize the main base, get a nearby worker
            var availableGas = GetUnits(NeutralUnits, Units.GasGeysers, onlyVisible: true)
                .FirstOrDefault(gas => UnitUtils.IsResourceManaged(gas) && !UnitUtils.IsGasExploited(gas));

            if (availableGas == null) {
                return RequestResult.NoSuitableLocation;
            }

            producer.PlaceExtractor(buildingType, availableGas);
            UnitModule.Get<CapacityModule>(availableGas).Assign(producer); // Assign the worker until extractor is spawned
            BuildingTracker.ConfirmPlacement(buildingType, availableGas.Position, producer);
        }
        else if (location != default) {
            if (!BuildingTracker.CanPlace(buildingType, location)) {
                return RequestResult.NoSuitableLocation;
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

        return RequestResult.Ok;
    }

    public static bool CanPlace(uint unitType, Vector3 targetPos) {
        return BuildingTracker.CanPlace(unitType, targetPos);
    }

    public static RequestResult ResearchUpgrade(uint upgradeType) {
        var producer = GetAvailableProducer(upgradeType, allowQueue: true);

        return ResearchUpgrade(upgradeType, producer);
    }

    public static RequestResult ResearchUpgrade(uint upgradeType, Unit producer) {
        var researchTypeData = KnowledgeBase.GetUpgradeData(upgradeType);

        var requirementsValidationResult = ValidateRequirements(upgradeType, producer, researchTypeData);
        if (requirementsValidationResult != RequestResult.Ok) {
            return requirementsValidationResult;
        }

        producer.ResearchUpgrade(upgradeType);

        AvailableMinerals -= researchTypeData.MineralCost;
        AvailableVespene -= researchTypeData.VespeneCost;

        return RequestResult.Ok;
    }

    public static RequestResult UpgradeInto(uint buildingType) {
        var producer = GetAvailableProducer(buildingType);

        return UpgradeInto(buildingType, producer);
    }

    public static RequestResult UpgradeInto(uint buildingType, Unit producer) {
        var buildingTypeData = KnowledgeBase.GetUnitTypeData(buildingType);

        var requirementsValidationResult = ValidateRequirements(buildingType, producer, buildingTypeData);
        if (requirementsValidationResult != RequestResult.Ok) {
            return requirementsValidationResult;
        }

        producer.UpgradeInto(buildingType);

        AvailableMinerals -= buildingTypeData.MineralCost;
        AvailableVespene -= buildingTypeData.VespeneCost;

        return RequestResult.Ok;
    }

    public static RequestResult PlaceExpand(uint buildingType) {
        if (!MapAnalyzer.IsInitialized) {
            return RequestResult.NotSupported;
        }

        var expandLocation = GetFreeExpandLocations()
            .OrderBy(expandLocation => Pathfinder.FindPath(StartingTownHall.Position, expandLocation).Count)
            .FirstOrDefault(expandLocation => BuildingTracker.CanPlace(buildingType, expandLocation));

        if (expandLocation == default) {
            return RequestResult.NoSuitableLocation;
        }

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

    public static RequestResult CanAfford(int mineralCost, int vespeneCost)
    {
        if (AvailableMinerals < mineralCost) {
            return RequestResult.NotEnoughMinerals;
        }

        if (AvailableVespene < vespeneCost) {
            return RequestResult.NotEnoughVespeneGas;
        }

        return RequestResult.Ok;
    }

    public static bool IsUnlocked(uint unitType) {
        if (Units.Prerequisites.TryGetValue(unitType, out var prerequisiteUnitType)) {
            return GetUnits(OwnedUnits, prerequisiteUnitType, onlyCompleted: true).Any();
        }

        return true;
    }

    public static bool HasEnoughSupply(float foodCost) {
        return AvailableSupply >= foodCost;
    }

    public static IEnumerable<Unit> GetMiningTownHalls() {
        return GetUnits(OwnedUnits, Units.Hatchery)
            .Where(townHall => MapAnalyzer.ExpandLocations.Any(expandLocation => townHall.DistanceTo(expandLocation) < ExpandIsTakenRadius));
    }

    // TODO GD Implement a more robust check
    public static IEnumerable<Vector3> GetFreeExpandLocations() {
        return MapAnalyzer.ExpandLocations
            .Where(expandLocation => !GetUnits(OwnedUnits, Units.TownHalls).Any(townHall => townHall.DistanceTo(expandLocation) < ExpandIsTakenRadius))
            .Where(expandLocation => !GetUnits(EnemyUnits, Units.TownHalls).Any(townHall => townHall.DistanceTo(expandLocation) < ExpandIsTakenRadius))
            .Where(expandLocation => !GetUnits(NeutralUnits, Units.Destructibles).Any(destructible => destructible.DistanceTo(expandLocation) < ExpandIsTakenRadius));
    }
}
