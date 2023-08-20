using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Sajuuk.Builds.BuildRequests;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.Utils;
using SC2APIProtocol;

namespace Sajuuk;

public class Controller : IController {
    private const int RealTime = (int)(1000 / TimeUtils.FasterFramesPerSecond);
    private int _frameDelayMs = 0;

    private const float ExpandIsTakenRadius = 4f;

    private readonly IUnitsTracker _unitsTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly TechTree _techTree; // TODO GD There's probably a circular dependency with tech tree
    private readonly KnowledgeBase _knowledgeBase;
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
        IRegionsTracker regionsTracker,
        TechTree techTree,
        KnowledgeBase knowledgeBase,
        IChatService chatService,
        List<INeedUpdating> trackers
    ) {
        _unitsTracker = unitsTracker;
        _regionsTracker = regionsTracker;
        _techTree = techTree;
        _knowledgeBase = knowledgeBase;
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

    public bool Spend(int mineralCost = 0, int vespeneCost = 0, float foodCost = 0) {
        if (CanAfford(mineralCost, vespeneCost) != BuildRequestResult.Ok || !HasEnoughSupply(foodCost)) {
            return false;
        }

        AvailableMinerals -= mineralCost;
        AvailableVespene -= vespeneCost;
        CurrentSupply += Convert.ToUInt32(Math.Ceiling(foodCost)); // Round up zerglings food, will be corrected on next frame

        return true;
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
    public BuildRequestResult CanAfford(int mineralCost, int vespeneCost) {
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

    public bool HasEnoughSupply(float foodCost) {
        return AvailableSupply >= foodCost;
    }

    public IEnumerable<Unit> GetMiningTownHalls() {
        return _unitsTracker.GetUnits(_unitsTracker.OwnedUnits, Units.Hatchery)
            .Where(townHall => _regionsTracker.ExpandLocations.Any(expandLocation => townHall.DistanceTo(expandLocation.Position) < ExpandIsTakenRadius));
    }

    public Point GetCurrentCameraLocation() {
        return Observation.Observation.RawData.Player.Camera;
    }
}
