using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sajuuk.ExtensionMethods;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.Managers.EconomyManagement.TownHallSupervision;
using Sajuuk.MapAnalysis;
using SC2APIProtocol;

namespace Sajuuk.Builds.BuildRequests.Fulfillment;

public class BuildRequestFulfiller : IBuildRequestFulfiller {
    private record struct FulfillmentResult(BuildRequestResult BuildRequestResult, IBuildRequestFulfillment BuildRequestFulfillment);

    private readonly TechTree _techTree;
    private readonly KnowledgeBase _knowledgeBase;
    private readonly IUnitsTracker _unitsTracker;
    private readonly IBuildingTracker _buildingTracker;
    private readonly IPathfinder _pathfinder;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IController _controller;
    private readonly IRegionsTracker _regionsTracker;
    private readonly IBuildRequestFulfillmentFactory _buildRequestFulfillmentFactory;
    private readonly IBuildRequestFulfillmentTracker _buildRequestFulfillmentTracker;

    private const float ExpandIsTakenRadius = 4f;

    public BuildRequestFulfiller(
        TechTree techTree,
        KnowledgeBase knowledgeBase,
        IUnitsTracker unitsTracker,
        IBuildingTracker buildingTracker,
        IPathfinder pathfinder,
        ITerrainTracker terrainTracker,
        IController controller,
        IRegionsTracker regionsTracker,
        IBuildRequestFulfillmentFactory buildRequestFulfillmentFactory,
        IBuildRequestFulfillmentTracker buildRequestFulfillmentTracker
    ) {
        _techTree = techTree;
        _knowledgeBase = knowledgeBase;
        _unitsTracker = unitsTracker;
        _buildingTracker = buildingTracker;
        _pathfinder = pathfinder;
        _terrainTracker = terrainTracker;
        _controller = controller;
        _regionsTracker = regionsTracker;
        _buildRequestFulfillmentFactory = buildRequestFulfillmentFactory;
        _buildRequestFulfillmentTracker = buildRequestFulfillmentTracker;
    }

    public BuildRequestResult FulfillBuildRequest(IFulfillableBuildRequest buildRequest) {
        var result = buildRequest.BuildType switch
        {
            BuildType.Train => TrainUnit(buildRequest.UnitOrUpgradeType),
            BuildType.Build => PlaceBuilding(buildRequest.UnitOrUpgradeType),
            //BuildType.Research => ResearchUpgrade(buildRequest.UnitOrUpgradeType, buildRequest.AllowQueueing),
            //BuildType.UpgradeInto => UpgradeInto(buildRequest.UnitOrUpgradeType),
            BuildType.Expand => PlaceExpand(buildRequest.UnitOrUpgradeType),
            _ => new FulfillmentResult(BuildRequestResult.NotSupported, null)
        };

        if (result.BuildRequestResult == BuildRequestResult.Ok) {
            Logger.Info($"Fulfilled 1 quantity of build request \"{buildRequest}\"");
            buildRequest.AddFulfillment(result.BuildRequestFulfillment);
            _buildRequestFulfillmentTracker.TrackFulfillment(result.BuildRequestFulfillment);
        }

        return result.BuildRequestResult;
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

        var canAffordResult = _controller.CanAfford(mineralCost, vespeneCost);
        if (canAffordResult != BuildRequestResult.Ok) {
            result |= canAffordResult;
        }

        if (!_controller.HasEnoughSupply(foodCost)) {
            result |= BuildRequestResult.NotEnoughSupply;
        }

        if (!_controller.IsUnlocked(unitOrUpgradeType, prerequisites)) {
            result |= BuildRequestResult.TechRequirementsNotMet;
        }

        return result;
    }

    /// <summary>
    /// Trains a unit of the given unitType, if possible.
    /// </summary>
    /// <param name="unitType">The unit type to train.</param>
    /// <returns>A BuildRequestResult that describes if the unit could be trained, or why not.</returns>
    private FulfillmentResult TrainUnit(uint unitType) {
        var producer = GetAvailableProducer(unitType);

        return TrainUnit(unitType, producer);
    }

    /// <summary>
    /// Trains a unit of the given unitType using the given producer, if possible.
    /// </summary>
    /// <param name="unitType">The unit type to train.</param>
    /// <param name="producer">The unit to use as a producer.</param>
    /// <returns>A BuildRequestResult that describes if the unit could be trained, or why not.</returns>
    private FulfillmentResult TrainUnit(uint unitType, Unit producer) {
        var unitTypeData = _knowledgeBase.GetUnitTypeData(unitType);

        var requirementsValidationResult = ValidateRequirements(unitType, producer, unitTypeData);
        if (requirementsValidationResult != BuildRequestResult.Ok) {
            return new FulfillmentResult(requirementsValidationResult, null);
        }

        var order = producer.TrainUnit(unitType);
        if (order == null) {
            return new FulfillmentResult(BuildRequestResult.NotSupported, null);
        }

        _controller.Spend((int)unitTypeData.MineralCost, (int)unitTypeData.VespeneCost, unitTypeData.FoodRequired);
        var fulfillment = _buildRequestFulfillmentFactory.CreateTrainUnitFulfillment(producer, order, unitType);

        return new FulfillmentResult(BuildRequestResult.Ok, fulfillment);
    }

    /// <summary>
    /// Places a building of the given buildingType at the given location, if possible.
    /// If no location is given, one will be determined.
    /// </summary>
    /// <param name="buildingType">The building type to build.</param>
    /// <param name="location">The location to build on. If no location is given, one will be determined.</param>
    /// <returns>A BuildRequestResult that describes if the building could be placed, or why not.</returns>
    private FulfillmentResult PlaceBuilding(uint buildingType, Vector2 location = default) {
        var producer = GetAvailableProducer(buildingType);

        return PlaceBuilding(buildingType, producer, location);
    }

    /// <summary>
    /// Places a building of the given buildingType at the given location using the given producer, if possible.
    /// If no location is given, one will be determined.
    /// </summary>
    /// <param name="buildingType">The building type to build.</param>
    /// <param name="producer">The producer to use to place the building.</param>
    /// <param name="location">The location to build on. If no location is given, one will be determined.</param>
    /// <returns>A BuildRequestResult that describes if the building could be placed, or why not.</returns>
    private FulfillmentResult PlaceBuilding(uint buildingType, Unit producer, Vector2 location = default) {
        var buildingTypeData = _knowledgeBase.GetUnitTypeData(buildingType);

        var requirementsValidationResult = ValidateRequirements(buildingType, producer, buildingTypeData);
        if (requirementsValidationResult != BuildRequestResult.Ok) {
            return new FulfillmentResult(requirementsValidationResult, null);
        }

        UnitOrder order;
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
                return new FulfillmentResult(BuildRequestResult.NoSuitableLocation, null);
            }

            producer = GetAvailableProducer(buildingType, closestTo: availableGas.Position.ToVector2());
            order = producer.PlaceExtractor(buildingType, availableGas);
            _buildingTracker.ConfirmPlacement(buildingType, availableGas.Position.ToVector2(), producer);
        }
        else if (location != default) {
            Logger.Debug("Trying to build {0} with location {1}", buildingTypeData.Name, location);
            if (!_buildingTracker.CanPlace(buildingType, location)) {
                return new FulfillmentResult(BuildRequestResult.NoSuitableLocation, null);
            }

            producer = GetAvailableProducer(buildingType, closestTo: location);
            order = producer.PlaceBuilding(buildingType, location);
            _buildingTracker.ConfirmPlacement(buildingType, location, producer);
        }
        else {
            Logger.Debug("Trying to build {0} without location", buildingTypeData.Name);
            var constructionSpot = _buildingTracker.FindConstructionSpot(buildingType);
            if (constructionSpot == default) {
                return new FulfillmentResult(BuildRequestResult.NoSuitableLocation, null);
            }

            producer = GetAvailableProducer(buildingType, closestTo: constructionSpot);
            order = producer.PlaceBuilding(buildingType, constructionSpot);
            _buildingTracker.ConfirmPlacement(buildingType, constructionSpot, producer);
        }

        _controller.Spend((int)buildingTypeData.MineralCost, (int)buildingTypeData.VespeneCost);
        var fulfillment = _buildRequestFulfillmentFactory.CreatePlaceBuildingFulfillment(producer, order, buildingType);

        return new FulfillmentResult(BuildRequestResult.Ok, fulfillment);
    }

    /// <summary>
    /// Research the given upgrade, if possible.
    /// </summary>
    /// <param name="upgradeType">The upgrade type to research.</param>
    /// <param name="allowQueue">Whether we allow queuing the research if no producer is available right now.</param>
    /// <returns>A BuildRequestResult that describes if the upgrade could be researched, or why not.</returns>
    private BuildRequestResult ResearchUpgrade(uint upgradeType, bool allowQueue) {
        var producer = GetAvailableProducer(upgradeType, allowQueue);

        return ResearchUpgrade(upgradeType, producer);
    }

    /// <summary>
    /// Research the given upgrade with the given producer, if possible.
    /// </summary>
    /// <param name="upgradeType">The upgrade type to research.</param>
    /// <param name="producer">The producer to use to research the upgrade.</param>
    /// <returns>A BuildRequestResult that describes if the upgrade could be researched, or why not.</returns>
    private BuildRequestResult ResearchUpgrade(uint upgradeType, Unit producer) {
        var researchTypeData = _knowledgeBase.GetUpgradeData(upgradeType);

        var requirementsValidationResult = ValidateRequirements(upgradeType, producer, researchTypeData);
        if (requirementsValidationResult != BuildRequestResult.Ok) {
            return requirementsValidationResult;
        }

        producer.ResearchUpgrade(upgradeType);

        _controller.Spend((int)researchTypeData.MineralCost, (int)researchTypeData.VespeneCost);

        return BuildRequestResult.Ok;
    }

    /// <summary>
    /// Upgrades a building into another one, if possible.
    /// </summary>
    /// <param name="buildingType">The building type to upgrade into.</param>
    /// <returns>A BuildRequestResult that describes if the upgrade could be done, or why not.</returns>
    private BuildRequestResult UpgradeInto(uint buildingType) {
        var producer = GetAvailableProducer(buildingType);

        return UpgradeInto(buildingType, producer);
    }

    /// <summary>
    /// Upgrades a building into another one, if possible.
    /// </summary>
    /// <param name="buildingType">The building type to upgrade into.</param>
    /// <param name="producer">The producer to use to upgrade into the given building type.</param>
    /// <returns>A BuildRequestResult that describes if the upgrade could be done, or why not.</returns>
    private BuildRequestResult UpgradeInto(uint buildingType, Unit producer) {
        var buildingTypeData = _knowledgeBase.GetUnitTypeData(buildingType);

        var requirementsValidationResult = ValidateRequirements(buildingType, producer, buildingTypeData);
        if (requirementsValidationResult != BuildRequestResult.Ok) {
            return requirementsValidationResult;
        }

        producer.UpgradeInto(buildingType);

        _controller.Spend((int)buildingTypeData.MineralCost, (int)buildingTypeData.VespeneCost);

        return BuildRequestResult.Ok;
    }

    /// <summary>
    /// Places an expand, if possible.
    /// The best expand location will be determined for you.
    /// </summary>
    /// <param name="buildingType">The type of townhall to place.</param>
    /// <returns>A BuildRequestResult that describes if the expand could be placed, or why not.</returns>
    private FulfillmentResult PlaceExpand(uint buildingType) {
        var producer = GetAvailableProducer(buildingType);

        return PlaceExpand(buildingType, producer);
    }

    /// <summary>
    /// Places an expand using the given producer, if possible.
    /// The best expand location will be determined for you.
    /// </summary>
    /// <param name="buildingType">The type of townhall to place.</param>
    /// <param name="producer">The producer to use to place the expand.</param>
    /// <returns>A BuildRequestResult that describes if the expand could be placed, or why not.</returns>
    private FulfillmentResult PlaceExpand(uint buildingType, Unit producer) {
        var buildingTypeData = _knowledgeBase.GetUnitTypeData(buildingType);
        var requirementsValidationResult = ValidateRequirements(buildingType, producer, buildingTypeData);
        if (requirementsValidationResult != BuildRequestResult.Ok) {
            return new FulfillmentResult(requirementsValidationResult, null);
        }

        var expandLocation = GetFreeExpandLocations()
            .Where(expandLocation => _pathfinder.FindPath(_terrainTracker.StartingLocation, expandLocation) != null)
            .OrderBy(expandLocation => _pathfinder.FindPath(_terrainTracker.StartingLocation, expandLocation).Count)
            .FirstOrDefault(expandLocation => _buildingTracker.CanPlace(buildingType, expandLocation));

        if (expandLocation == default) {
            return new FulfillmentResult(BuildRequestResult.NoSuitableLocation, null);
        }

        return PlaceBuilding(buildingType, producer, expandLocation);
    }

    /// <summary>
    /// Gets all the expand locations that are free.
    /// TODO GD Implement a more robust check and move this to an ExpandTracker
    /// </summary>
    /// <returns>The list of expand locations available for expansion.</returns>
    private IEnumerable<Vector2> GetFreeExpandLocations() {
        return _regionsTracker.ExpandLocations
            .Select(expandLocation => expandLocation.Position)
            .Where(expandLocation => !_unitsTracker.GetUnits(_unitsTracker.OwnedUnits, Units.TownHalls).Any(townHall => townHall.DistanceTo(expandLocation) < ExpandIsTakenRadius))
            .Where(expandLocation => !_unitsTracker.GetUnits(_unitsTracker.EnemyUnits, Units.TownHalls).Any(townHall => townHall.DistanceTo(expandLocation) < ExpandIsTakenRadius))
            .Where(expandLocation => !_unitsTracker.GetUnits(_unitsTracker.NeutralUnits, Units.Destructibles).Any(destructible => destructible.DistanceTo(expandLocation) < ExpandIsTakenRadius));
    }
}
