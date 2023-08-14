using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using Sajuuk.ExtensionMethods;
using Sajuuk.Builds;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.Managers.EconomyManagement.TownHallSupervision;
using Sajuuk.MapAnalysis;
using Sajuuk.Utils;
using SC2APIProtocol;

namespace Sajuuk;

public class Controller : IController {
    private const int RealTime = (int)(1000 / TimeUtils.FramesPerSecond);
    private int _frameDelayMs = 0;

    private const float ExpandIsTakenRadius = 4f;

    private readonly IUnitsTracker _unitsTracker;
    private readonly IBuildingTracker _buildingTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly TechTree _techTree; // TODO GD There's probably a circular dependency with tech tree
    private readonly KnowledgeBase _knowledgeBase;
    private readonly IPathfinder _pathfinder;
    private readonly IChatService _chatService;
    private readonly List<INeedUpdating> _trackers;

    public ResponseGameInfo GameInfo { get; private set; }
    public ResponseObservation Observation { get; private set; }

    public uint SupportedSupply { get; private set; }
    public uint CurrentSupply { get; private set; }
    public int AvailableSupply => (int)(SupportedSupply - CurrentSupply);

    public bool IsSupplyBlocked => AvailableSupply <= 0;

    public int AvailableMinerals { get; private set; }
    public int AvailableVespene { get; private set; }

    public HashSet<uint> ResearchedUpgrades { get; private set; }

    public Controller(
        IUnitsTracker unitsTracker,
        IBuildingTracker buildingTracker,
        ITerrainTracker terrainTracker,
        IRegionsTracker regionsTracker,
        TechTree techTree,
        KnowledgeBase knowledgeBase,
        IPathfinder pathfinder,
        IChatService chatService,
        List<INeedUpdating> trackers
    ) {
        _unitsTracker = unitsTracker;
        _buildingTracker = buildingTracker;
        _terrainTracker = terrainTracker;
        _regionsTracker = regionsTracker;
        _techTree = techTree;
        _knowledgeBase = knowledgeBase;
        _pathfinder = pathfinder;
        _chatService = chatService;

        _trackers = trackers;
    }

    public void SetSimulationTime(string reason) {
        if (_frameDelayMs == 0) {
            return;
        }

        _frameDelayMs = 0;

        _chatService.Chat($"Simulation time set: {reason}", toTeam: true);
    }

    public void SetRealTime(string reason) {
        if (_frameDelayMs == RealTime) {
            return;
        }

        _frameDelayMs = RealTime;

        _chatService.Chat($"Real time set: {reason}", toTeam: true);
    }

    public void NewFrame(ResponseGameInfo gameInfo, ResponseObservation observation) {
        GameInfo = gameInfo;
        Observation = observation;

        if (!IsProperlyInitialized()) {
            Environment.Exit(1);
        }

        foreach (var tracker in _trackers) {
            tracker.Update(Observation, GameInfo);
        }

        CurrentSupply = Observation.Observation.PlayerCommon.FoodUsed;
        var hasOddAmountOfZerglings = _unitsTracker.OwnedUnits.Count(unit => unit.UnitType == Units.Zergling) % 2 == 1;
        if (hasOddAmountOfZerglings) {
            // Zerglings have 0.5 supply. The api returns the supply rounded down, but the game considers the supply rounded up.
            CurrentSupply += 1;
        }

        SupportedSupply = Observation.Observation.PlayerCommon.FoodCap;

        AvailableMinerals = (int)Observation.Observation.PlayerCommon.Minerals;
        AvailableVespene = (int)Observation.Observation.PlayerCommon.Vespene;
        ResearchedUpgrades = new HashSet<uint>(Observation.Observation.RawData.Player.UpgradeIds);

        if (Program.DebugEnabled && _frameDelayMs > 0) {
            Thread.Sleep(_frameDelayMs);
        }
    }

    private bool IsProperlyInitialized() {
        if (GameInfo == null) {
            Logger.Error("GameInfo is null! The application will terminate.");
            return false;
        }

        if (_knowledgeBase.Data == null) {
            Logger.Error("TypeData is null! The application will terminate.");
            return false;
        }

        if (Observation == null) {
            Logger.Error("ResponseObservation is null! The application will terminate.");
            return false;
        }

        return true;
    }

    private int GetTotalCount(uint unitType) {
        var pendingCount = GetPendingCount(unitType, inConstruction: false);
        var constructionCount = _unitsTracker.GetUnits(_unitsTracker.OwnedUnits, unitType).Count();

        return pendingCount + constructionCount;
    }

    private int GetPendingCount(uint unitType, bool inConstruction = true) {
        var workers = _unitsTracker.GetUnits(_unitsTracker.OwnedUnits, Units.Workers);
        var abilityId = _knowledgeBase.GetUnitTypeData(unitType).AbilityId;

        var counter = 0;

        // Count workers that have been sent to build this structure
        foreach (var worker in workers) {
            if (worker.Orders.Any(order => order.AbilityId == abilityId)) {
                counter += 1;
            }
        }

        // Count buildings that are already in construction
        if (inConstruction) {
            foreach (var unit in _unitsTracker.GetUnits(_unitsTracker.OwnedUnits, unitType)) {
                if (!unit.IsOperational) {
                    counter += 1;
                }
            }
        }

        return counter;
    }

    /// <summary>
    /// Gets an available producer to produce the given unit or ability type.
    /// If the producers are drones and we only have 1 left, we will not return a producer.
    /// There might be a case where we want to use the last drone for some cheeky plays, but we'll cross that bridge when we get there.
    /// </summary>
    /// <param name="unitOrAbilityType">The unit or ability type to produce.</param>
    /// <param name="allowQueue">Whether to include produces that are already producing something else.</param>
    /// <param name="closestTo">A location to use to help select an appropriate producer. If not defined, will choose a producer regardless of their location.</param>
    /// <returns>The best producer, or null is none is available.</returns>
    /// <exception cref="ArgumentException">If the tech tree does not contain an entry for the given unit or ability type to produce.</exception>
    private Unit GetAvailableProducer(uint unitOrAbilityType, bool allowQueue = false, Vector2 closestTo = default) {
        if (!_techTree.Producer.ContainsKey(unitOrAbilityType)) {
            // TODO GD It could be an upgrade and not a unit.
            throw new ArgumentException($"Producer for unit {_knowledgeBase.GetUnitTypeData(unitOrAbilityType).Name} not found");
        }

        var possibleProducersUnitType = _techTree.Producer[unitOrAbilityType];
        var producers = _unitsTracker.GetUnits(_unitsTracker.OwnedUnits, possibleProducersUnitType)
            .Where(unit => unit.IsOperational && unit.IsAvailable)
            .ToList();

        if (possibleProducersUnitType == Units.Drone && producers.Count == 1) {
            // Do not use the last drone.
            return null;
        }

        if (!allowQueue) {
            producers = producers.Where(unit => !unit.OrdersExceptMining.Any()).ToList();
        }

        if (closestTo == default) {
            return producers.MinBy(unit => unit.OrdersExceptMining.Count());
        }

        // This can be tricked by impassable terrain, but looks good enough
        return producers.MinBy(producer => producer.DistanceTo(closestTo));
    }

    public BuildRequestResult FulfillBuildRequest(IFulfillableBuildRequest buildRequest) {
        var result = buildRequest.BuildType switch
        {
            BuildType.Train => TrainUnit(buildRequest.UnitOrUpgradeType),
            BuildType.Build => PlaceBuilding(buildRequest.UnitOrUpgradeType),
            BuildType.Research => ResearchUpgrade(buildRequest.UnitOrUpgradeType, buildRequest.AllowQueueing),
            BuildType.UpgradeInto => UpgradeInto(buildRequest.UnitOrUpgradeType),
            BuildType.Expand => PlaceExpand(buildRequest.UnitOrUpgradeType),
            _ => BuildRequestResult.NotSupported
        };

        if (result == BuildRequestResult.Ok) {
            Logger.Info($"(Controller) Completed build request {buildRequest}");
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
    private BuildRequestResult ValidateRequirements(uint unitType, Unit producer, UnitTypeData unitTypeData) {
        return ValidateRequirements(unitType, producer, (int)unitTypeData.MineralCost, (int)unitTypeData.VespeneCost, _techTree.UnitPrerequisites, unitTypeData.FoodRequired);
    }

    /// <summary>
    /// Validates if upgrade requirements are met based on the provided upgrade data.
    /// </summary>
    /// <param name="upgradeType">The upgrade type to produce</param>
    /// <param name="producer">The producer for the upgrade</param>
    /// <param name="upgradeData">The upgrade data describing costs</param>
    /// <returns>The appropriate BuildRequestResult flags describing the requirements validation</returns>
    private BuildRequestResult ValidateRequirements(uint upgradeType, Unit producer, UpgradeData upgradeData) {
        return ValidateRequirements(upgradeType, producer, (int)upgradeData.MineralCost, (int)upgradeData.VespeneCost, _techTree.UpgradePrerequisites);
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
    private BuildRequestResult ValidateRequirements(
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

    private BuildRequestResult TrainUnit(uint unitType) {
        var producer = GetAvailableProducer(unitType);

        return TrainUnit(unitType, producer);
    }

    private BuildRequestResult TrainUnit(uint unitType, Unit producer) {
        var unitTypeData = _knowledgeBase.GetUnitTypeData(unitType);

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

    private BuildRequestResult PlaceBuilding(uint buildingType, Vector2 location = default) {
        var producer = GetAvailableProducer(buildingType);

        return PlaceBuilding(buildingType, producer, location);
    }

    private BuildRequestResult PlaceBuilding(uint buildingType, Unit producer, Vector2 location = default) {
        var buildingTypeData = _knowledgeBase.GetUnitTypeData(buildingType);

        var requirementsValidationResult = ValidateRequirements(buildingType, producer, buildingTypeData);
        if (requirementsValidationResult != BuildRequestResult.Ok) {
            return requirementsValidationResult;
        }

        if (buildingType == Units.Extractor) {
            Logger.Debug("Trying to build {0}", buildingTypeData.Name);

            var extractorPositions = _unitsTracker.GetUnits(_unitsTracker.OwnedUnits, Units.Extractors)
                .Select(extractor => extractor.Position.ToVector2())
                .ToHashSet();

            var availableGas = _unitsTracker.GetUnits(_unitsTracker.NeutralUnits, Units.GasGeysers)
                .Where(gas => gas.Supervisor != null)
                .Where(gas => _buildingTracker.CanPlace(buildingType, gas.Position.ToVector2()))
                .Where(gas => !extractorPositions.Contains(gas.Position.ToVector2()))
                .MaxBy(gas => (gas.Supervisor as TownHallSupervisor)!.WorkerCount); // This is not cute nor clean, but it is efficient and we like that

            if (availableGas == null) {
                Logger.Debug("(Controller) No available gasses for extractor");
                return BuildRequestResult.NoSuitableLocation;
            }

            producer = GetAvailableProducer(buildingType, closestTo: availableGas.Position.ToVector2());
            producer.PlaceExtractor(buildingType, availableGas);
            _buildingTracker.ConfirmPlacement(buildingType, availableGas.Position.ToVector2(), producer);
        }
        else if (location != default) {
            Logger.Debug("Trying to build {0} with location {1}", buildingTypeData.Name, location);
            if (!_buildingTracker.CanPlace(buildingType, location)) {
                return BuildRequestResult.NoSuitableLocation;
            }

            producer = GetAvailableProducer(buildingType, closestTo: location);
            producer.PlaceBuilding(buildingType, location);
            _buildingTracker.ConfirmPlacement(buildingType, location, producer);
        }
        else {
            Logger.Debug("Trying to build {0} without location", buildingTypeData.Name);
            var constructionSpot = _buildingTracker.FindConstructionSpot(buildingType);
            if (constructionSpot == default) {
                return BuildRequestResult.NoSuitableLocation;
            }

            producer = GetAvailableProducer(buildingType, closestTo: constructionSpot);
            producer.PlaceBuilding(buildingType, constructionSpot);
            _buildingTracker.ConfirmPlacement(buildingType, constructionSpot, producer);
        }

        Logger.Debug("Done building {0}", buildingTypeData.Name);

        AvailableMinerals -= (int)buildingTypeData.MineralCost;
        AvailableVespene -= (int)buildingTypeData.VespeneCost;

        return BuildRequestResult.Ok;
    }

    private BuildRequestResult ResearchUpgrade(uint upgradeType, bool allowQueue) {
        var producer = GetAvailableProducer(upgradeType, allowQueue);

        return ResearchUpgrade(upgradeType, producer);
    }

    private BuildRequestResult ResearchUpgrade(uint upgradeType, Unit producer) {
        var researchTypeData = _knowledgeBase.GetUpgradeData(upgradeType);

        var requirementsValidationResult = ValidateRequirements(upgradeType, producer, researchTypeData);
        if (requirementsValidationResult != BuildRequestResult.Ok) {
            return requirementsValidationResult;
        }

        producer.ResearchUpgrade(upgradeType);

        AvailableMinerals -= (int)researchTypeData.MineralCost;
        AvailableVespene -= (int)researchTypeData.VespeneCost;

        return BuildRequestResult.Ok;
    }

    private BuildRequestResult UpgradeInto(uint buildingType) {
        var producer = GetAvailableProducer(buildingType);

        return UpgradeInto(buildingType, producer);
    }

    private BuildRequestResult UpgradeInto(uint buildingType, Unit producer) {
        var buildingTypeData = _knowledgeBase.GetUnitTypeData(buildingType);

        var requirementsValidationResult = ValidateRequirements(buildingType, producer, buildingTypeData);
        if (requirementsValidationResult != BuildRequestResult.Ok) {
            return requirementsValidationResult;
        }

        producer.UpgradeInto(buildingType);

        AvailableMinerals -= (int)buildingTypeData.MineralCost;
        AvailableVespene -= (int)buildingTypeData.VespeneCost;

        return BuildRequestResult.Ok;
    }

    private BuildRequestResult PlaceExpand(uint buildingType) {
        var producer = GetAvailableProducer(buildingType);

        return PlaceExpand(buildingType, producer);
    }

    private BuildRequestResult PlaceExpand(uint buildingType, Unit producer) {
        var buildingTypeData = _knowledgeBase.GetUnitTypeData(buildingType);
        var requirementsValidationResult = ValidateRequirements(buildingType, producer, buildingTypeData);
        if (requirementsValidationResult != BuildRequestResult.Ok) {
            return requirementsValidationResult;
        }

        var expandLocation = GetFreeExpandLocations()
            .Where(expandLocation => _pathfinder.FindPath(_terrainTracker.StartingLocation, expandLocation) != null)
            .OrderBy(expandLocation => _pathfinder.FindPath(_terrainTracker.StartingLocation, expandLocation).Count)
            .FirstOrDefault(expandLocation => _buildingTracker.CanPlace(buildingType, expandLocation));

        if (expandLocation == default) {
            return BuildRequestResult.NoSuitableLocation;
        }

        return PlaceBuilding(buildingType, producer, expandLocation);
    }

    public float GetResearchProgress(uint upgradeId) {
        var upgradeAbilityId = _knowledgeBase.GetUpgradeData(upgradeId).AbilityId;

        var upgradeOrder = _unitsTracker.GetUnits(_unitsTracker.OwnedUnits, _techTree.Producer[upgradeId])
            .SelectMany(producer => producer.Orders)
            .FirstOrDefault(order => order.AbilityId == upgradeAbilityId);

        if (upgradeOrder == null) {
            return -1;
        }

        return upgradeOrder.Progress;
    }

    public bool IsResearchInProgress(uint upgradeId) {
        var upgradeAbilityId = _knowledgeBase.GetUpgradeData(upgradeId).AbilityId;

        return _unitsTracker.GetUnits(_unitsTracker.OwnedUnits, _techTree.Producer[upgradeId])
            .Any(producer => producer.Orders.Any(order => order.AbilityId == upgradeAbilityId));
    }

    /**
     * Returns all producers currently carrying production orders.
     * This includes eggs hatching, units morphing and workers going to build.
     */
    public IEnumerable<Unit> GetProducersCarryingOrders(uint unitTypeToProduce) {
        // We add eggs because larvae become eggs and I don't want to add eggs to _techTree.Producer since they're not the original producer
        var potentialProducers = new HashSet<uint> { _techTree.Producer[unitTypeToProduce], Units.Egg };

        return _unitsTracker.GetUnits(_unitsTracker.OwnedUnits, potentialProducers).Where(producer => producer.IsProducing(unitTypeToProduce));
    }

    public IEnumerable<Effect> GetEffects(int effectId) {
        return Observation.Observation.RawData.Effects.Where(effect => effect.EffectId == effectId);
    }

    /// <summary>
    /// Indicates whether or not you can afford the given mineral and vespene cost.
    /// </summary>
    /// <param name="mineralCost">The mineral cost we want to pay</param>
    /// <param name="vespeneCost">The vespene cost we want to pay</param>
    /// <returns>The corresponding BuildRequestResult flags</returns>
    private BuildRequestResult CanAfford(int mineralCost, int vespeneCost) {
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
    public bool IsUnlocked(uint unitOrUpgradeType, Dictionary<uint, List<IPrerequisite>> prerequisites) {
        if (prerequisites.TryGetValue(unitOrUpgradeType, out var unitOrUpgradePrerequisites)) {
            return unitOrUpgradePrerequisites.All(prerequisite => prerequisite.IsMet(_unitsTracker.OwnedUnits, ResearchedUpgrades));
        }

        return true;
    }

    private bool HasEnoughSupply(float foodCost) {
        return AvailableSupply >= foodCost;
    }

    public IEnumerable<Unit> GetMiningTownHalls() {
        return _unitsTracker.GetUnits(_unitsTracker.OwnedUnits, Units.Hatchery)
            .Where(townHall => _regionsTracker.ExpandLocations.Any(expandLocation => townHall.DistanceTo(expandLocation.Position) < ExpandIsTakenRadius));
    }

    // TODO GD Implement a more robust check
    private IEnumerable<Vector2> GetFreeExpandLocations() {
        return _regionsTracker.ExpandLocations
            .Select(expandLocation => expandLocation.Position)
            .Where(expandLocation => !_unitsTracker.GetUnits(_unitsTracker.OwnedUnits, Units.TownHalls).Any(townHall => townHall.DistanceTo(expandLocation) < ExpandIsTakenRadius))
            .Where(expandLocation => !_unitsTracker.GetUnits(_unitsTracker.EnemyUnits, Units.TownHalls).Any(townHall => townHall.DistanceTo(expandLocation) < ExpandIsTakenRadius))
            .Where(expandLocation => !_unitsTracker.GetUnits(_unitsTracker.NeutralUnits, Units.Destructibles).Any(destructible => destructible.DistanceTo(expandLocation) < ExpandIsTakenRadius));
    }

    public Point GetCurrentCameraLocation() {
        return Observation.Observation.RawData.Player.Camera;
    }
}
