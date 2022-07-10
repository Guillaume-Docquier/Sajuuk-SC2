using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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

    private static UnitsTracker _unitsTracker;

    public static ResponseGameInfo GameInfo;
    public static ResponseObservation Obs; // TODO GD Make this private and add a setter

    public static ulong Frame = ulong.MaxValue;

    public static uint CurrentSupply;
    public static uint MaxSupply;

    public static int AvailableMinerals;
    public static int AvailableVespene;

    public static readonly List<Vector3> EnemyLocations = new List<Vector3>();
    public static readonly List<string> ChatLog = new List<string>();

    public static uint AvailableSupply => MaxSupply - CurrentSupply;
    public static List<Unit> OwnedUnits => _unitsTracker.OwnedUnits;
    public static List<Unit> NewOwnedUnits => _unitsTracker.NewOwnedUnits;
    public static List<Unit> DeadOwnedUnits => _unitsTracker.DeadOwnedUnits;
    public static List<Unit> NeutralUnits => _unitsTracker.NeutralUnits;
    public static List<Unit> EnemyUnits => _unitsTracker.EnemyUnits;

    public static void Pause() {
        Console.WriteLine("Press any key to continue...");
        while (Console.ReadKey().Key != ConsoleKey.Enter) {
            //do nothing
        }
    }

    public static ulong SecsToFrames(int seconds) {
        return (ulong)(FramesPerSecond * seconds);
    }

    public static void OpenFrame() {
        Frame = Obs.Observation.GameLoop;

        if (GameInfo == null || GameData.Data == null || Obs == null) {
            if (GameInfo == null) {
                Logger.Info("GameInfo is null! The application will terminate.");
            }
            else if (GameData.Data == null) {
                Logger.Info("GameData is null! The application will terminate.");
            }
            else {
                Logger.Info("ResponseObservation is null! The application will terminate.");
            }

            Pause();
            Environment.Exit(0);
        }

        if (_unitsTracker == null) {
            _unitsTracker = new UnitsTracker(Obs.Observation.RawData.Units, Frame);
        }
        else {
            _unitsTracker.Update(Obs.Observation.RawData.Units.ToList(), Frame);
        }

        Actions.Clear();

        foreach (var chat in Obs.Chat) {
            ChatLog.Add(chat.Message);
        }

        CurrentSupply = Obs.Observation.PlayerCommon.FoodUsed;
        MaxSupply = Obs.Observation.PlayerCommon.FoodCap;

        AvailableMinerals = Obs.Observation.PlayerCommon.Minerals;
        AvailableVespene = Obs.Observation.PlayerCommon.Vespene;

        if (Frame == 0) {
            var townHalls = GetUnits(OwnedUnits, Units.ResourceCenters).ToList();
            if (townHalls.Count > 0) {
                var townHall = townHalls[0];

                foreach (var startLocation in GameInfo.StartRaw.StartLocations) {
                    var enemyLocation = new Vector3(startLocation.X, startLocation.Y, 0);
                    if (townHall.DistanceTo(enemyLocation) > 30) {
                        EnemyLocations.Add(enemyLocation);
                    }
                }
            }

            // TODO GD Find expansion slots
        }
    }

    public static IEnumerable<Action> CloseFrame() {
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
        var abilityId = GameData.GetUnitTypeData(unitType).AbilityId;

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

    // This is a blocking call! Use it sparingly, or you will slow down your execution significantly!
    public static bool CanPlace(uint unitType, Vector3 targetPos) {
        var abilityId = GameData.GetUnitTypeData(unitType).AbilityId;

        var queryBuildingPlacement = new RequestQueryBuildingPlacement
        {
            AbilityId = (int)abilityId, // TODO GD Can I just sync the types?
            TargetPos = new Point2D
            {
                X = targetPos.X,
                Y = targetPos.Y
            }
        };

        var requestQuery = new Request
        {
            Query = new RequestQuery()
        };
        requestQuery.Query.Placements.Add(queryBuildingPlacement);

        var result = Program.GameConnection.SendQuery(requestQuery.Query);
        if (result.Result.Placements.Count > 0) {
            if (result.Result.Placements.Count > 1) {
                Logger.Warning("[CanPlace] Expected 1 placement, found {0}", result.Result.Placements.Count);
            }

            var actionResult = result.Result.Placements[0].Result;
            if (actionResult == ActionResult.NotSupported) {
                Debugger.AddSquare(targetPos.ToPoint(), 1f, Colors.Black);
            }
            else if (actionResult == ActionResult.CantBuildLocationInvalid) {
                Debugger.AddSquare(targetPos.ToPoint(), 1f, Colors.Red);
            }
            else if (actionResult == ActionResult.CantBuildTooCloseToResources) {
                Debugger.AddSquare(targetPos.ToPoint(), 1f, Colors.Cyan);
            }
            else if (actionResult == ActionResult.Success) {
                Debugger.AddSquare(targetPos.ToPoint(), 1f, Colors.Green);
            }
            else {
                Logger.Warning("[CanPlace] Unexpected placement result: {0}", actionResult);
                Debugger.AddSquare(targetPos.ToPoint(), 1f, Colors.Magenta);
            }

            return actionResult == ActionResult.Success;
        }

        if (result.Result.Placements.Count > 1) {
            Logger.Warning("[CanPlace] Expected 1 placement, found 0");
        }

        return false;
    }

    // TODO GD Get rid?
    private static bool IsInRange(Vector3 targetPosition, List<Unit> units, float maxDistance) {
        return GetFirstInRange(targetPosition, units, maxDistance) != null;
    }

    // TODO GD Get rid?
    private static Unit GetFirstInRange(Vector3 targetPosition, List<Unit> units, float maxDistance) {
        //squared distance is faster to calculate
        var maxDistanceSqr = maxDistance * maxDistance;
        foreach (var unit in units) {
            if (Vector3.DistanceSquared(targetPosition, unit.Position) <= maxDistanceSqr) {
                return unit;
            }
        }

        return null;
    }

    // TODO GD Get rid?
    private static Vector3 FindConstructionSpot(uint buildingType) {
        Vector3 startingSpot;

        var resourceCenters = GetUnits(OwnedUnits, Units.ResourceCenters).ToList();
        if (resourceCenters.Count > 0)
            startingSpot = resourceCenters[0].Position;
        else {
            Logger.Error("Unable to construct: {0}. No resource center was found.", GameData.GetUnitTypeData(buildingType).Name);

            return Vector3.Zero;
        }

        const int radius = 12;

        //trying to find a valid construction spot
        var mineralFields = GetUnits(OwnedUnits, Units.MineralFields, onlyVisible: true).ToList();
        Vector3 constructionSpot;
        while (true) {
            constructionSpot = new Vector3(startingSpot.X + Random.Next(-radius, radius + 1), startingSpot.Y + Random.Next(-radius, radius + 1), 0);

            //avoid building in the mineral line
            if (IsInRange(constructionSpot, mineralFields, 5)) {
                continue;
            }

            //check if the building fits
            if (!CanPlace(buildingType, constructionSpot)) {
                continue;
            }

            //ok, we found a spot
            break;
        }

        return constructionSpot;
    }

    /*
     * OKAY!
     */

    public static Unit GetAvailableProducer(uint unitOrAbilityType) {
        if (!Units.Producers.ContainsKey(unitOrAbilityType)) {
            throw new NotImplementedException($"Producer for unit {GameData.GetUnitTypeData(unitOrAbilityType).Name} not found");
        }

        var possibleProducers = Units.Producers[unitOrAbilityType];

        return GetUnits(OwnedUnits, possibleProducers, onlyCompleted: true)
            .FirstOrDefault(unit => unit.Orders.Count(order => order.AbilityId != Abilities.DroneGather && order.AbilityId != Abilities.DroneReturnCargo) == 0);
    }

    public static bool ExecuteBuildStep(BuildOrders.BuildStep buildStep) {
        switch (buildStep.BuildType) {
            case BuildType.Train:
                return TrainUnit(buildStep.UnitOrUpgradeType);
            case BuildType.Build:
                return PlaceBuilding(buildStep.UnitOrUpgradeType);
            case BuildType.Research:
                return ResearchUpgrade(buildStep.UnitOrUpgradeType);
            case BuildType.UpgradeInto:
                return UpgradeInto(buildStep.UnitOrUpgradeType);
        }

        return false;
    }

    public static bool TrainUnit(uint unitType) {
        var producer = GetAvailableProducer(unitType);

        return TrainUnit(unitType, producer);
    }

    public static bool TrainUnit(uint unitType, Unit producer)
    {
        var unitTypeData = GameData.GetUnitTypeData(unitType);
        if (producer == null || !CanAfford(unitTypeData) || !HasEnoughSupply(unitType) || !IsUnlocked(unitType)) {
            return false;
        }

        producer.TrainUnit(unitType);

        AvailableMinerals -= unitTypeData.MineralCost;
        AvailableVespene -= unitTypeData.VespeneCost;

        return true;
    }

    public static bool PlaceBuilding(uint buildingType) {
        var producer = GetAvailableProducer(buildingType);

        return PlaceBuilding(buildingType, producer);
    }

    public static bool PlaceBuilding(uint buildingType, Unit producer) {
        var buildingTypeData = GameData.GetUnitTypeData(buildingType);
        if (producer == null || !CanAfford(buildingTypeData) || !IsUnlocked(buildingType)) {
            return false;
        }

        if (buildingType == Units.Extractor) {
            var availableGas = GetUnits(NeutralUnits, Units.GasGeysers, onlyVisible: true)
                .FirstOrDefault(gas => UnitUtils.IsResourceManaged(gas) && !UnitUtils.IsGasExploited(gas));

            if (availableGas == null) {
                return false;
            }

            producer.PlaceExtractor(buildingType, availableGas);
            CapacityModule.Assign(availableGas, producer); // Assign the worker until extractor is spawned
        }
        else {
            var constructionSpot = FindConstructionSpot(buildingType);

            producer.PlaceBuilding(buildingType, constructionSpot);
        }

        AvailableMinerals -= buildingTypeData.MineralCost;
        AvailableVespene -= buildingTypeData.VespeneCost;

        return true;
    }

    public static bool ResearchUpgrade(uint upgradeType) {
        var producer = GetAvailableProducer(upgradeType);

        return ResearchUpgrade(upgradeType, producer);
    }

    public static bool ResearchUpgrade(uint upgradeType, Unit producer) {
        var researchTypeData = GameData.GetUpgradeData(upgradeType);
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
        var buildingTypeData = GameData.GetUnitTypeData(buildingType);
        if (producer == null || !CanAfford(buildingTypeData) || !IsUnlocked(buildingType)) {
            return false;
        }

        producer.UpgradeInto(buildingType);

        AvailableMinerals -= buildingTypeData.MineralCost;
        AvailableVespene -= buildingTypeData.VespeneCost;

        return true;
    }

    public static IList<Unit> GetAvailableLarvae() {
        return GetUnits(OwnedUnits, Units.Larva, onlyCompleted: true).Where(larva => larva.Orders.Count == 0).ToList();
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
        return AvailableSupply >= GameData.GetUnitTypeData(unitType).FoodRequired;
    }
}
