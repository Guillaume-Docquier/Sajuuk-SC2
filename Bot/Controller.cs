using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using Bot.Builds;
using Bot.Debugging;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.GameSense.EnemyStrategyTracking;
using Bot.GameSense.RegionTracking;
using Bot.Managers.EconomyManagement.TownHallSupervision;
using Bot.MapKnowledge;
using Bot.Utils;
using Bot.Wrapper;
using SC2APIProtocol;
using Action = SC2APIProtocol.Action;

namespace Bot;

public static class Controller {
    private const int RealTime = (int)(1000 / TimeUtils.FramesPerSecond);
    private static int _frameDelayMs = 0;

    private static readonly List<Action> Actions = new List<Action>();
    public const float ExpandIsTakenRadius = 4f;

    public static ResponseGameInfo GameInfo { get; private set; }
    public static Race EnemyRace { get; private set; }
    public static ResponseObservation Observation { get; private set; }

    public static uint Frame { get; private set; } = uint.MaxValue;

    public static uint CurrentSupply { get; private set; }
    public static uint MaxSupply { get; private set; }
    public static int AvailableSupply => (int)(MaxSupply - CurrentSupply);

    public static bool IsSupplyCapped => AvailableSupply == 0;

    public static int AvailableMinerals { get; private set; }
    public static int AvailableVespene { get; private set; }

    public static HashSet<uint> ResearchedUpgrades { get; private set; }

    private static List<INeedUpdating> ThoseWhoNeedUpdating => new List<INeedUpdating>
    {
        IncomeTracker.Instance,         // Depends on nothing
        ChatTracker.Instance,           // Depends on nothing
        VisibilityTracker.Instance,     // Depends on nothing

        UnitsTracker.Instance,          // Depends on VisibilityTracker
        DebuggingFlagsTracker.Instance, // Depends on ChatTracker

        MapAnalyzer.Instance,           // Depends on UnitsTracker and VisibilityTracker

        CreepTracker.Instance,          // Depends on VisibilityTracker and MapAnalyzer
        BuildingTracker.Instance,       // Depends on UnitsTracker and MapAnalyzer
        ExpandAnalyzer.Instance,        // Depends on UnitsTracker and MapAnalyzer
        RegionAnalyzer.Instance,        // Depends on ExpandAnalyzer and MapAnalyzer

        EnemyStrategyTracker.Instance,  // Depends on UnitsTracker, ExpandAnalyzer and RegionAnalyzer
        RegionTracker.Instance,         // Depends on VisibilityTracker, UnitsTracker, MapAnalyzer and RegionAnalyzer
    };

    public static void Reset() {
        Frame = uint.MaxValue;
        Actions.Clear();

        GameInfo = null;
        Observation = null;
        EnemyRace = default;

        _frameDelayMs = 0;
        ThoseWhoNeedUpdating.ForEach(needsUpdating => needsUpdating.Reset());
    }

    public static void SetRealTime(string reason) {
        _frameDelayMs = RealTime;

        Chat($"Real time set: {reason}", toTeam: true);
    }

    private static void UpdateEnemyRace() {
        if (EnemyRace == default) {
            EnemyRace = GameInfo.PlayerInfo
                .Where(playerInfo => playerInfo.Type != PlayerType.Observer)
                .First(playerInfo => playerInfo.PlayerId != Observation.Observation.PlayerCommon.PlayerId)
                .RaceRequested;
        }

        if (EnemyRace == Race.Random) {
            var enemyUnits = UnitsTracker.EnemyUnits;
            if (enemyUnits.Any(worker => Units.AllTerranUnits.Contains(worker.UnitType))) {
                EnemyRace = Race.Terran;
            }
            else if (enemyUnits.Any(worker => Units.AllProtossUnits.Contains(worker.UnitType))) {
                EnemyRace = Race.Protoss;
            }
            else if (enemyUnits.Any(worker => Units.AllZergUnits.Contains(worker.UnitType))) {
                EnemyRace = Race.Zerg;
            }
        }
    }

    public static void NewFrame(ResponseGameInfo gameInfo, ResponseObservation observation) {
        Actions.Clear();

        GameInfo = gameInfo;
        Observation = observation;
        Frame = Observation.Observation.GameLoop;

        if (!IsProperlyInitialized()) {
            Environment.Exit(0);
        }

        UpdateEnemyRace();

        foreach (var needsUpdating in ThoseWhoNeedUpdating) {
            needsUpdating.Update(Observation);
        }

        CurrentSupply = Observation.Observation.PlayerCommon.FoodUsed;
        var hasOddAmountOfZerglings = UnitsTracker.OwnedUnits.Count(unit => unit.UnitType == Units.Zergling) % 2 == 1;
        if (hasOddAmountOfZerglings) {
            // Zerglings have 0.5 supply. The api returns the supply rounded down, but the game considers the supply rounded up.
            CurrentSupply += 1;
        }

        MaxSupply = Observation.Observation.PlayerCommon.FoodCap;

        AvailableMinerals = (int)Observation.Observation.PlayerCommon.Minerals;
        AvailableVespene = (int)Observation.Observation.PlayerCommon.Vespene;
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

    public static Unit GetAvailableProducer(uint unitOrAbilityType, bool allowQueue = false, Vector2 closestTo = default) {
        if (!TechTree.Producer.ContainsKey(unitOrAbilityType)) {
            throw new NotImplementedException($"Producer for unit {KnowledgeBase.GetUnitTypeData(unitOrAbilityType).Name} not found");
        }

        var possibleProducers = TechTree.Producer[unitOrAbilityType];

        var producers = GetUnits(UnitsTracker.OwnedUnits, possibleProducers).Where(unit => unit.IsOperational && unit.IsAvailable);

        if (!allowQueue) {
            producers = producers.Where(unit => !unit.OrdersExceptMining.Any());
        }

        if (closestTo == default) {
            return producers.MinBy(unit => unit.OrdersExceptMining.Count());
        }

        // This can be tricked by impassable terrain, but looks good enough
        return producers.MinBy(producer => producer.DistanceTo(closestTo));
    }

    // TODO GD Should use an IBuildStep, probably. BuildFulfillment seems odd here
    public static BuildRequestResult ExecuteBuildStep(BuildFulfillment buildStep) {
        var result = buildStep.BuildType switch
        {
            BuildType.Train => TrainUnit(buildStep.UnitOrUpgradeType),
            BuildType.Build => PlaceBuilding(buildStep.UnitOrUpgradeType),
            BuildType.Research => ResearchUpgrade(buildStep.UnitOrUpgradeType, buildStep.Queue),
            BuildType.UpgradeInto => UpgradeInto(buildStep.UnitOrUpgradeType),
            BuildType.Expand => PlaceExpand(buildStep.UnitOrUpgradeType),
            _ => BuildRequestResult.NotSupported
        };

        if (result == BuildRequestResult.Ok) {
            Logger.Info("(Controller) Completed build step {0}", buildStep);
        }

        return result;
    }

    /// <summary>
    /// Validates if unit requirements are met based on the provided unit type data.
    /// </summary>
    /// <param name="unitType">The unit type to produce</param>
    /// <param name="producer">The producer for the unit</param>
    /// <param name="unitTypeData">The unit type data describing costs</param>
    /// <returns>The appropriate BuildRequestResult flags describing the requirements validation</returns>
    private static BuildRequestResult ValidateRequirements(uint unitType, Unit producer, UnitTypeData unitTypeData) {
        return ValidateRequirements(unitType, producer, (int)unitTypeData.MineralCost, (int)unitTypeData.VespeneCost, TechTree.UnitPrerequisites, unitTypeData.FoodRequired);
    }

    /// <summary>
    /// Validates if upgrade requirements are met based on the provided upgrade data.
    /// </summary>
    /// <param name="upgradeType">The upgrade type to produce</param>
    /// <param name="producer">The producer for the upgrade</param>
    /// <param name="upgradeData">The upgrade data describing costs</param>
    /// <returns>The appropriate BuildRequestResult flags describing the requirements validation</returns>
    private static BuildRequestResult ValidateRequirements(uint upgradeType, Unit producer, UpgradeData upgradeData) {
        return ValidateRequirements(upgradeType, producer, (int)upgradeData.MineralCost, (int)upgradeData.VespeneCost, TechTree.UpgradePrerequisites);
    }

    /// <summary>
    /// Validates if the given requirements are met.
    /// </summary>
    /// <param name="unitOrUpgradeType">The unit or upgrade type to produce</param>
    /// <param name="producer">The producer for the unit or upgrade</param>
    /// <param name="mineralCost">The mineral cost of the unit or upgrade</param>
    /// <param name="vespeneCost">The vespene cost of the unit or upgrade</param>
    /// <param name="prerequisites">The tech requirements</param>
    /// <param name="foodCost">The food cost of the unit or upgrade</param>
    /// <returns>The appropriate BuildRequestResult flags describing the requirements validation</returns>
    private static BuildRequestResult ValidateRequirements(
        uint unitOrUpgradeType,
        Unit producer,
        int mineralCost,
        int vespeneCost,
        Dictionary<uint, List<IPrerequisite>> prerequisites,
        float foodCost = 0
    ) {
        var result = BuildRequestResult.Ok;

        if (producer == null) {
            result |= BuildRequestResult.NoProducersAvailable;
        }

        var canAffordResult = CanAfford(mineralCost, vespeneCost);
        if (canAffordResult != BuildRequestResult.Ok) {
            result |= canAffordResult;
        }

        if (!HasEnoughSupply(foodCost)) {
            result |= BuildRequestResult.NotEnoughSupply;
        }

        if (!IsUnlocked(unitOrUpgradeType, prerequisites)) {
            result |= BuildRequestResult.TechRequirementsNotMet;
        }

        return result;
    }

    private static BuildRequestResult TrainUnit(uint unitType) {
        var producer = GetAvailableProducer(unitType);

        return TrainUnit(unitType, producer);
    }

    private static BuildRequestResult TrainUnit(uint unitType, Unit producer) {
        var unitTypeData = KnowledgeBase.GetUnitTypeData(unitType);

        var requirementsValidationResult = ValidateRequirements(unitType, producer, unitTypeData);
        if (requirementsValidationResult != BuildRequestResult.Ok) {
            return requirementsValidationResult;
        }

        producer.TrainUnit(unitType);

        AvailableMinerals -= (int)unitTypeData.MineralCost;
        AvailableVespene -= (int)unitTypeData.VespeneCost;
        CurrentSupply += Convert.ToUInt32(Math.Ceiling(unitTypeData.FoodRequired)); // Round up zerglings food, will be corrected on next frame

        return BuildRequestResult.Ok;
    }

    private static BuildRequestResult PlaceBuilding(uint buildingType, Vector2 location = default) {
        var producer = GetAvailableProducer(buildingType);

        return PlaceBuilding(buildingType, producer, location);
    }

    private static BuildRequestResult PlaceBuilding(uint buildingType, Unit producer, Vector2 location = default) {
        var buildingTypeData = KnowledgeBase.GetUnitTypeData(buildingType);

        var requirementsValidationResult = ValidateRequirements(buildingType, producer, buildingTypeData);
        if (requirementsValidationResult != BuildRequestResult.Ok) {
            return requirementsValidationResult;
        }

        if (buildingType == Units.Extractor) {
            Logger.Debug("Trying to build {0}", buildingTypeData.Name);

            var extractorPositions = GetUnits(UnitsTracker.OwnedUnits, Units.Extractors)
                .Select(extractor => extractor.Position.ToVector2())
                .ToHashSet();

            var availableGas = GetUnits(UnitsTracker.NeutralUnits, Units.GasGeysers)
                .Where(gas => gas.Supervisor != null)
                .Where(gas => BuildingTracker.CanPlace(buildingType, gas.Position.ToVector2()))
                .Where(gas => !extractorPositions.Contains(gas.Position.ToVector2()))
                .MaxBy(gas => (gas.Supervisor as TownHallSupervisor)!.WorkerCount); // This is not cute nor clean, but it is efficient and we like that

            if (availableGas == null) {
                Logger.Warning("(Controller) No available gasses for extractor");
                return BuildRequestResult.NoSuitableLocation;
            }

            producer = GetAvailableProducer(buildingType, closestTo: availableGas.Position.ToVector2());
            producer.PlaceExtractor(buildingType, availableGas);
            BuildingTracker.ConfirmPlacement(buildingType, availableGas.Position.ToVector2(), producer);
        }
        else if (location != default) {
            Logger.Debug("Trying to build {0} with location {1}", buildingTypeData.Name, location);
            if (!BuildingTracker.CanPlace(buildingType, location)) {
                return BuildRequestResult.NoSuitableLocation;
            }

            producer = GetAvailableProducer(buildingType, closestTo: location);
            producer.PlaceBuilding(buildingType, location);
            BuildingTracker.ConfirmPlacement(buildingType, location, producer);
        }
        else {
            Logger.Debug("Trying to build {0} without location", buildingTypeData.Name);
            var constructionSpot = BuildingTracker.FindConstructionSpot(buildingType);
            if (constructionSpot == default) {
                return BuildRequestResult.NoSuitableLocation;
            }

            producer = GetAvailableProducer(buildingType, closestTo: constructionSpot);
            producer.PlaceBuilding(buildingType, constructionSpot);
            BuildingTracker.ConfirmPlacement(buildingType, constructionSpot, producer);
        }

        Logger.Debug("Done building {0}", buildingTypeData.Name);

        AvailableMinerals -= (int)buildingTypeData.MineralCost;
        AvailableVespene -= (int)buildingTypeData.VespeneCost;

        return BuildRequestResult.Ok;
    }

    private static BuildRequestResult ResearchUpgrade(uint upgradeType, bool allowQueue) {
        var producer = GetAvailableProducer(upgradeType, allowQueue);

        return ResearchUpgrade(upgradeType, producer);
    }

    private static BuildRequestResult ResearchUpgrade(uint upgradeType, Unit producer) {
        var researchTypeData = KnowledgeBase.GetUpgradeData(upgradeType);

        var requirementsValidationResult = ValidateRequirements(upgradeType, producer, researchTypeData);
        if (requirementsValidationResult != BuildRequestResult.Ok) {
            return requirementsValidationResult;
        }

        producer.ResearchUpgrade(upgradeType);

        AvailableMinerals -= (int)researchTypeData.MineralCost;
        AvailableVespene -= (int)researchTypeData.VespeneCost;

        return BuildRequestResult.Ok;
    }

    private static BuildRequestResult UpgradeInto(uint buildingType) {
        var producer = GetAvailableProducer(buildingType);

        return UpgradeInto(buildingType, producer);
    }

    private static BuildRequestResult UpgradeInto(uint buildingType, Unit producer) {
        var buildingTypeData = KnowledgeBase.GetUnitTypeData(buildingType);

        var requirementsValidationResult = ValidateRequirements(buildingType, producer, buildingTypeData);
        if (requirementsValidationResult != BuildRequestResult.Ok) {
            return requirementsValidationResult;
        }

        producer.UpgradeInto(buildingType);

        AvailableMinerals -= (int)buildingTypeData.MineralCost;
        AvailableVespene -= (int)buildingTypeData.VespeneCost;

        return BuildRequestResult.Ok;
    }

    private static BuildRequestResult PlaceExpand(uint buildingType) {
        if (!MapAnalyzer.IsInitialized) {
            return BuildRequestResult.NotSupported;
        }

        var producer = GetAvailableProducer(buildingType);

        return PlaceExpand(buildingType, producer);
    }

    private static BuildRequestResult PlaceExpand(uint buildingType, Unit producer) {
        if (!MapAnalyzer.IsInitialized) {
            return BuildRequestResult.NotSupported;
        }

        var buildingTypeData = KnowledgeBase.GetUnitTypeData(buildingType);
        var requirementsValidationResult = ValidateRequirements(buildingType, producer, buildingTypeData);
        if (requirementsValidationResult != BuildRequestResult.Ok) {
            return requirementsValidationResult;
        }

        var expandLocation = GetFreeExpandLocations()
            .Where(expandLocation => Pathfinder.FindPath(MapAnalyzer.StartingLocation, expandLocation) != null)
            .OrderBy(expandLocation => Pathfinder.FindPath(MapAnalyzer.StartingLocation, expandLocation).Count)
            .FirstOrDefault(expandLocation => BuildingTracker.CanPlace(buildingType, expandLocation));

        if (expandLocation == default) {
            return BuildRequestResult.NoSuitableLocation;
        }

        return PlaceBuilding(buildingType, producer, expandLocation);
    }

    public static float GetResearchProgress(uint upgradeId) {
        var upgradeAbilityId = KnowledgeBase.GetUpgradeData(upgradeId).AbilityId;

        var upgradeOrder = GetUnits(UnitsTracker.OwnedUnits, TechTree.Producer[upgradeId])
            .SelectMany(producer => producer.Orders)
            .FirstOrDefault(order => order.AbilityId == upgradeAbilityId);

        if (upgradeOrder == null) {
            return -1;
        }

        return upgradeOrder.Progress;
    }

    public static bool IsResearchInProgress(uint upgradeId) {
        var upgradeAbilityId = KnowledgeBase.GetUpgradeData(upgradeId).AbilityId;

        return GetUnits(UnitsTracker.OwnedUnits, TechTree.Producer[upgradeId])
            .Any(producer => producer.Orders.Any(order => order.AbilityId == upgradeAbilityId));
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
     * Buildings that are in production are included.
     */
    public static IEnumerable<Unit> GetUnits(IEnumerable<Unit> unitPool, uint unitToGet) {
        return GetUnits(unitPool, new HashSet<uint>{ unitToGet });
    }

    /**
     * Returns all units that match a certain set of types from the provided unitPool, including units of equivalent types.
     * Buildings that are in production are included.
     */
    public static IEnumerable<Unit> GetUnits(IEnumerable<Unit> unitPool, HashSet<uint> unitTypesToGet, bool includeCloaked = false) {
        var equivalentUnitTypes = unitTypesToGet
            .Where(unitTypeToGet => Units.EquivalentTo.ContainsKey(unitTypeToGet))
            .SelectMany(unitTypeToGet => Units.EquivalentTo[unitTypeToGet])
            .ToList();

        unitTypesToGet.UnionWith(equivalentUnitTypes);

        foreach (var unit in unitPool) {
            if (!unitTypesToGet.Contains(unit.UnitType)) {
                continue;
            }

            if (unit.IsCloaked && !includeCloaked) {
                continue;
            }

            yield return unit;
        }
    }

    public static IEnumerable<Effect> GetEffects(int effectId) {
        return Observation.Observation.RawData.Effects.Where(effect => effect.EffectId == effectId);
    }

    /// <summary>
    /// Indicates whether or not you can afford the given mineral and vespene cost.
    /// </summary>
    /// <param name="mineralCost">The mineral cost we want to pay</param>
    /// <param name="vespeneCost">The vespene cost we want to pay</param>
    /// <returns>The corresponding BuildRequestResult flags</returns>
    private static BuildRequestResult CanAfford(int mineralCost, int vespeneCost) {
        var result = BuildRequestResult.Ok;

        if (AvailableMinerals < mineralCost) {
            result |= BuildRequestResult.NotEnoughMinerals;
        }

        if (AvailableVespene < vespeneCost) {
            result |= BuildRequestResult.NotEnoughVespeneGas;
        }

        return result;
    }

    /// <summary>
    /// Indicates whether or not the tech requirements for the given upgrade are met
    /// </summary>
    /// <param name="unitOrUpgradeType">The upgrade type you want to research</param>
    /// <param name="prerequisites">The tech requirements</param>
    /// <returns>True if the upgrade can be researched right now</returns>
    public static bool IsUnlocked(uint unitOrUpgradeType, Dictionary<uint, List<IPrerequisite>> prerequisites) {
        if (prerequisites.TryGetValue(unitOrUpgradeType, out var unitOrUpgradePrerequisites)) {
            return unitOrUpgradePrerequisites.All(prerequisite => prerequisite.IsMet());
        }

        return true;
    }

    private static bool HasEnoughSupply(float foodCost) {
        return AvailableSupply >= foodCost;
    }

    public static IEnumerable<Unit> GetMiningTownHalls() {
        return GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery)
            .Where(townHall => ExpandAnalyzer.ExpandLocations.Any(expandLocation => townHall.DistanceTo(expandLocation.Position) < ExpandIsTakenRadius));
    }

    // TODO GD Implement a more robust check
    public static IEnumerable<Vector2> GetFreeExpandLocations() {
        return ExpandAnalyzer.ExpandLocations
            .Select(expandLocation => expandLocation.Position)
            .Where(expandLocation => !GetUnits(UnitsTracker.OwnedUnits, Units.TownHalls).Any(townHall => townHall.DistanceTo(expandLocation) < ExpandIsTakenRadius))
            .Where(expandLocation => !GetUnits(UnitsTracker.EnemyUnits, Units.TownHalls).Any(townHall => townHall.DistanceTo(expandLocation) < ExpandIsTakenRadius))
            .Where(expandLocation => !GetUnits(UnitsTracker.NeutralUnits, Units.Destructibles).Any(destructible => destructible.DistanceTo(expandLocation) < ExpandIsTakenRadius));
    }

    public static Point GetCurrentCameraLocation() {
        return Observation.Observation.RawData.Player.Camera;
    }
}
