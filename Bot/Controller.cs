using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using Bot.Builds;
using Bot.GameData;
using Bot.GameSense;
using Bot.Managers;
using Bot.MapKnowledge;
using Bot.UnitModules;
using Bot.Wrapper;
using SC2APIProtocol;
using Action = SC2APIProtocol.Action;

namespace Bot;

public static class Controller {
    public enum RequestResult {
        Ok,
        NoProducersAvailable,
        NotEnoughMinerals,
        NotEnoughVespeneGas,
        NotEnoughSupply,
        NoSuitableLocation,
        TechRequirementsNotMet,
        NotSupported,
    }

    private const int RealTime = (int)(1000 / FramesPerSecond);
    private static int _frameDelayMs = 0;

    private static readonly List<Action> Actions = new List<Action>();
    private static readonly Random Random = new Random();
    public const double FramesPerSecond = 22.4;
    public const float ExpandIsTakenRadius = 4f;

    public static ResponseGameInfo GameInfo { get; private set; }
    public static ResponseObservation Observation { get; private set; }

    public static uint Frame { get; private set; } = uint.MaxValue;

    public static uint CurrentSupply { get; set; }
    public static uint MaxSupply;
    public static int AvailableSupply => (int)(MaxSupply - CurrentSupply);

    public static bool IsSupplyCapped => AvailableSupply == 0;

    public static int AvailableMinerals;
    public static int AvailableVespene;
    public static HashSet<uint> ResearchedUpgrades;

    public static readonly List<string> ChatLog = new List<string>();

    private static readonly List<INeedUpdating> ThoseWhoNeedUpdating = new List<INeedUpdating>
    {
        UnitsTracker.Instance, // Depends on nothing
        VisibilityTracker.Instance, // Depends on nothing
        CreepTracker.Instance, // Depends on nothing

        MapAnalyzer.Instance, // Depends on UnitsTracker

        ExpandAnalyzer.Instance, // Depends on UnitsTracker and MapAnalyzer
        BuildingTracker.Instance, // Depends on UnitsTracker and MapAnalyzer
    };

    public static void Pause() {
        Console.WriteLine("Press any key to continue...");
        while (Console.ReadKey().Key != ConsoleKey.Enter) {
            //do nothing
        }
    }

    public static void SetRealTime() {
        _frameDelayMs = RealTime;
    }

    public static ulong SecsToFrames(int seconds) {
        return (ulong)(FramesPerSecond * seconds);
    }

    public static void NewGameInfo(ResponseGameInfo gameInfo) {
        GameInfo = gameInfo;
    }

    public static void NewObservation(ResponseObservation observation) {
        Observation = observation;
        Frame = Observation.Observation.GameLoop;

        if (!IsProperlyInitialized()) {
            Pause();
            Environment.Exit(0);
        }

        ThoseWhoNeedUpdating.ForEach(needsUpdating => needsUpdating.Update(Observation));

        Actions.Clear();

        // This doesn't work? I don't really care but it would be nice if it did
        foreach (var chat in Observation.Chat) {
            ChatLog.Add(chat.Message);
        }

        CurrentSupply = Observation.Observation.PlayerCommon.FoodUsed;
        var hasOddAmountOfZerglings = UnitsTracker.OwnedUnits.Count(unit => unit.UnitType == Units.Zergling) % 2 == 1;
        if (hasOddAmountOfZerglings) {
            // Zerglings have 0.5 supply. The api returns the supply rounded down, but the game considers the supply rounded up.
            CurrentSupply += 1;
        }

        MaxSupply = Observation.Observation.PlayerCommon.FoodCap;

        AvailableMinerals = Observation.Observation.PlayerCommon.Minerals;
        AvailableVespene = Observation.Observation.PlayerCommon.Vespene;
        ResearchedUpgrades = new HashSet<uint>(Observation.Observation.RawData.Player.UpgradeIds);
    }

    private static bool IsProperlyInitialized() {
        if (GameInfo == null) {
            Logger.Error("GameInfo is null! The application will terminate.");
            return false;
        }

        if (KnowledgeBase.Data == null) {
            Logger.Error("TypeData is null! The application will terminate.");
            return false;
        }

        if (Observation == null) {
            Logger.Error("ResponseObservation is null! The application will terminate.");
            return false;
        }

        return true;
    }

    public static IEnumerable<Action> GetActions() {
        if (Program.DebugEnabled && _frameDelayMs > 0) {
            Thread.Sleep(_frameDelayMs);
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
        var constructionCount = GetUnits(UnitsTracker.OwnedUnits, unitType).Count();

        return pendingCount + constructionCount;
    }

    private static int GetPendingCount(uint unitType, bool inConstruction = true) {
        var workers = GetUnits(UnitsTracker.OwnedUnits, Units.Workers);
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
            foreach (var unit in GetUnits(UnitsTracker.OwnedUnits, unitType)) {
                if (!unit.IsOperational) {
                    counter += 1;
                }
            }
        }

        return counter;
    }

    public static Unit GetAvailableProducer(uint unitOrAbilityType, bool allowQueue = false) {
        if (!TechTree.Producer.ContainsKey(unitOrAbilityType)) {
            throw new NotImplementedException($"Producer for unit {KnowledgeBase.GetUnitTypeData(unitOrAbilityType).Name} not found");
        }

        var possibleProducers = TechTree.Producer[unitOrAbilityType];

        var producers = GetUnits(UnitsTracker.OwnedUnits, possibleProducers)
            .Where(unit => unit.IsOperational && unit.IsAvailable)
            .OrderBy(unit => unit.OrdersExceptMining.Count());

        if (!allowQueue) {
            return producers.FirstOrDefault(unit => !unit.OrdersExceptMining.Any());
        }

        return producers.FirstOrDefault();
    }

    // TODO GD Should use an IBuildStep, probably. BuildFulfillment seems odd here
    public static RequestResult ExecuteBuildStep(BuildFulfillment buildStep) {
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
            // TODO GD Get a nearby worker
            var availableGas = GetUnits(UnitsTracker.NeutralUnits, Units.GasGeysers)
                .Where(gas => gas.Supervisor != null && !UnitUtils.IsGasExploited(gas))
                .MaxBy(gas => (gas.Supervisor as TownHallManager)!.WorkerCount);

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
            if (constructionSpot == default) {
                return RequestResult.NoSuitableLocation;
            }

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

        var producer = GetAvailableProducer(buildingType);

        return PlaceExpand(buildingType, producer);
    }

    public static RequestResult PlaceExpand(uint buildingType, Unit producer) {
        if (!MapAnalyzer.IsInitialized) {
            return RequestResult.NotSupported;
        }

        var buildingTypeData = KnowledgeBase.GetUnitTypeData(buildingType);
        var requirementsValidationResult = ValidateRequirements(buildingType, producer, buildingTypeData);
        if (requirementsValidationResult != RequestResult.Ok) {
            return requirementsValidationResult;
        }

        var expandLocation = GetFreeExpandLocations()
            .Where(expandLocation => Pathfinder.FindPath(MapAnalyzer.StartingLocation, expandLocation) != null)
            .OrderBy(expandLocation => Pathfinder.FindPath(MapAnalyzer.StartingLocation, expandLocation).Count)
            .FirstOrDefault(expandLocation => BuildingTracker.CanPlace(buildingType, expandLocation));

        if (expandLocation == default) {
            return RequestResult.NoSuitableLocation;
        }

        return PlaceBuilding(buildingType, producer, expandLocation);
    }

    /**
     * Returns all producers currently carrying production orders.
     * This includes eggs hatching, units morphing and workers going to build.
     */
    public static IEnumerable<Unit> GetProducersCarryingOrders(uint unitTypeToProduce) {
        // We add eggs because larvae become eggs and I don't want to add eggs to TechTree.Producer since they're not the original producer
        var potentialProducers = new HashSet<uint> { TechTree.Producer[unitTypeToProduce], Units.Egg };

        return GetUnits(UnitsTracker.OwnedUnits, potentialProducers).Where(producer => producer.IsProducing(unitTypeToProduce));
    }

    /**
     * Returns all units of a certain type from the provided unitPool, including units of equivalent types.
     * Units that are in production are included.
     */
    public static IEnumerable<Unit> GetUnits(IEnumerable<Unit> unitPool, uint unitToGet) {
        return GetUnits(unitPool, new HashSet<uint>{ unitToGet });
    }

    /**
     * Returns all units that match a certain set of types from the provided unitPool, including units of equivalent types.
     * Units that are in production are included.
     */
    public static IEnumerable<Unit> GetUnits(IEnumerable<Unit> unitPool, HashSet<uint> unitsToGet) {
        var equivalentUnits = unitsToGet
            .Where(unitToGet => Units.EquivalentTo.ContainsKey(unitToGet))
            .SelectMany(unitToGet => Units.EquivalentTo[unitToGet])
            .ToList();

        unitsToGet.UnionWith(equivalentUnits);

        foreach (var unit in unitPool) {
            if (unitsToGet.Contains(unit.UnitType)) {
                yield return unit;
            }
        }
    }

    public static IEnumerable<Effect> GetEffects(int effectId) {
        return Observation.Observation.RawData.Effects.Where(effect => effect.EffectId == effectId);
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
        if (TechTree.Prerequisite.TryGetValue(unitType, out var prerequisiteUnitType)) {
            return GetUnits(UnitsTracker.OwnedUnits, prerequisiteUnitType).Any(unit => unit.IsOperational);
        }

        return true;
    }

    public static bool HasEnoughSupply(float foodCost) {
        return AvailableSupply >= foodCost;
    }

    public static IEnumerable<Unit> GetMiningTownHalls() {
        return GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery)
            .Where(townHall => ExpandAnalyzer.ExpandLocations.Any(expandLocation => townHall.DistanceTo(expandLocation) < ExpandIsTakenRadius));
    }

    // TODO GD Implement a more robust check
    public static IEnumerable<Vector3> GetFreeExpandLocations() {
        return ExpandAnalyzer.ExpandLocations
            .Where(expandLocation => !GetUnits(UnitsTracker.OwnedUnits, Units.TownHalls).Any(townHall => townHall.DistanceTo(expandLocation) < ExpandIsTakenRadius))
            .Where(expandLocation => !GetUnits(UnitsTracker.EnemyUnits, Units.TownHalls).Any(townHall => townHall.DistanceTo(expandLocation) < ExpandIsTakenRadius))
            .Where(expandLocation => !GetUnits(UnitsTracker.NeutralUnits, Units.Destructibles).Any(destructible => destructible.DistanceTo(expandLocation) < ExpandIsTakenRadius));
    }
}
