using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Wrapper;
using SC2APIProtocol;
using Action = SC2APIProtocol.Action;

namespace Bot;

public static class Controller {
    //editable
    private const int FrameDelay = 0; //too fast? increase this to e.g. 20

    //don't edit
    private static readonly List<Action> Actions = new List<Action>();
    private static readonly Random Random = new Random();
    private const double FramesPerSecond = 22.4;

    public static ResponseGameInfo GameInfo;
    public static ResponseData GameData; // TODO GD Refine this e.g. modify zerg unit costs
    public static ResponseObservation Obs; // TODO GD Make this private and add a setter

    public static ulong Frame = ulong.MaxValue;

    public static uint CurrentSupply;
    public static uint MaxSupply;
    public static uint AvailableSupply => MaxSupply - CurrentSupply;

    public static uint Minerals;
    public static uint Vespene;

    public static Dictionary<uint, Unit> OwnedUnitsMap;
    public static List<Unit> OwnedUnits;
    public static List<Unit> NeutralUnits;
    public static List<Unit> EnemyUnits;

    public static readonly List<Vector3> EnemyLocations = new List<Vector3>();
    public static readonly List<string> ChatLog = new List<string>();

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
        if (GameInfo == null || GameData == null || Obs == null) {
            if (GameInfo == null) {
                Logger.Info("GameInfo is null! The application will terminate.");
            }
            else if (GameData == null) {
                Logger.Info("GameData is null! The application will terminate.");
            }
            else {
                Logger.Info("ResponseObservation is null! The application will terminate.");
            }

            Pause();
            Environment.Exit(0);
        }

        Actions.Clear();

        foreach (var chat in Obs.Chat) {
            ChatLog.Add(chat.Message);
        }

        Frame = Obs.Observation.GameLoop;

        CurrentSupply = Obs.Observation.PlayerCommon.FoodUsed;
        MaxSupply = Obs.Observation.PlayerCommon.FoodCap;

        Minerals = Obs.Observation.PlayerCommon.Minerals;
        Vespene = Obs.Observation.PlayerCommon.Vespene;

        OwnedUnits = Obs.Observation.RawData.Units.Where(unit => unit.Alliance == Alliance.Self).Select(unit => new Unit(unit)).ToList();
        NeutralUnits = Obs.Observation.RawData.Units.Where(unit => unit.Alliance == Alliance.Neutral).Select(unit => new Unit(unit)).ToList();
        EnemyUnits = Obs.Observation.RawData.Units.Where(unit => unit.Alliance == Alliance.Enemy).Select(unit => new Unit(unit)).ToList();

        //initialization
        if (Frame == 0) {
            var resourceCenters = GetUnits(OwnedUnits, Units.ResourceCenters).ToList();
            if (resourceCenters.Count > 0) {
                var rcPosition = resourceCenters[0].Position;

                foreach (var startLocation in GameInfo.StartRaw.StartLocations) {
                    var enemyLocation = new Vector3(startLocation.X, startLocation.Y, 0);
                    var distance = Vector3.Distance(enemyLocation, rcPosition);
                    if (distance > 30) {
                        EnemyLocations.Add(enemyLocation);
                    }
                }
            }
        }
    }

    public static IEnumerable<Action> CloseFrame() {
        return Actions;
    }

    public static string GetUnitName(uint unitType) {
        return GameData.Units[(int)unitType].Name;
    }

    public static void AddAction(Action action) {
        Actions.Add(action);
    }

    public static void Chat(string message, bool toTeam = false) {
        AddAction(ActionBuilder.Chat(message, toTeam));
    }

    public static void Attack(IEnumerable<Unit> units, Vector3 target) {
        AddAction(ActionBuilder.Attack(units.Select(unit => unit.Tag), target));
    }

    private static int GetTotalCount(uint unitType) {
        var pendingCount = GetPendingCount(unitType, inConstruction: false);
        var constructionCount = GetUnits(OwnedUnits, unitType).Count();

        return pendingCount + constructionCount;
    }

    private static int GetPendingCount(uint unitType, bool inConstruction = true) {
        var workers = GetUnits(OwnedUnits, Units.Workers);
        var abilityId = Abilities.GetId(unitType);

        var counter = 0;

        //count workers that have been sent to build this structure
        foreach (var worker in workers) {
            if (worker.Order.AbilityId == abilityId) {
                counter += 1;
            }
        }

        //count buildings that are already in construction
        if (inConstruction) {
            foreach (var unit in GetUnits(OwnedUnits, unitType)) {
                if (unit.BuildProgress < 1) {
                    counter += 1;
                }
            }
        }

        return counter;
    }

    // TODO GD Get rid?
    private static bool CanConstruct(uint unitType) {
        //is it a structure?
        if (Units.Structures.Contains(unitType)) {
            //we need worker for every structure
            if (!GetUnits(OwnedUnits, Units.Workers).Any()) {
                return false;
            }

            //we need an RC for any structure
            var resourceCenters = GetUnits(OwnedUnits, Units.ResourceCenters, onlyCompleted: true);
            if (!resourceCenters.Any()) {
                return false;
            }

            if ((unitType == Units.CommandCenter) || (unitType == Units.SupplyDepot)) {
                return CanAfford(unitType);
            }

            //we need supply depots for the following structures
            var depots = GetUnits(OwnedUnits, Units.SupplyDepots, onlyCompleted: true);
            if (!depots.Any()) {
                return false;
            }

            if (unitType == Units.Barracks) {
                return CanAfford(unitType);
            }
        }

        //it's an actual unit
        else {
            //do we have enough supply?
            var requiredSupply = Controller.GameData.Units[(int)unitType].FoodRequired;
            if (requiredSupply > (MaxSupply - CurrentSupply)) {
                return false;
            }

            //do we construct the units from barracks?
            if (Units.FromBarracks.Contains(unitType)) {
                var barracks = GetUnits(OwnedUnits, Units.Barracks, onlyCompleted: true);
                if (!barracks.Any()) {
                    return false;
                }
            }
        }

        return CanAfford(unitType);
    }

    // TODO GD Get rid?
    private static int GetAbilityId(uint unit)
    {
        return (int)GameData.Units[(int)unit].AbilityId;
    }

    // TODO GD Get rid?
    private static bool CanPlace(uint unitType, Vector3 targetPos) {
        //Note: this is a blocking call! Use it sparingly, or you will slow down your execution significantly!
        var abilityId = Abilities.GetId(unitType);

        var queryBuildingPlacement = new RequestQueryBuildingPlacement
        {
            AbilityId = abilityId,
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
            return (result.Result.Placements[0].Result == ActionResult.Success);
        }

        return false;
    }

    // TODO GD Get rid?
    private static void DistributeWorkers() {
        var workers = GetUnits(OwnedUnits, Units.Workers);
        var idleWorkers = new List<Unit>();
        foreach (var worker in workers) {
            if (worker.Order.AbilityId != 0) {
                continue;
            }

            idleWorkers.Add(worker);
        }

        if (idleWorkers.Count > 0) {
            var resourceCenters = GetUnits(OwnedUnits, Units.ResourceCenters, onlyCompleted: true);
            var mineralFields = GetUnits(OwnedUnits, Units.MineralFields, onlyVisible: true).ToList();

            foreach (var rc in resourceCenters) {
                //get one of the closer mineral fields
                var mf = GetFirstInRange(rc.Position, mineralFields, 7);
                if (mf == null) {
                    continue;
                }

                //only one at a time
                Logger.Info("Distributing idle worker: {0}", idleWorkers[0].Tag);
                idleWorkers[0].Smart(mf);

                return;
            }

            //nothing to be done
            return;
        }
        else {
            //let's see if we can distribute between bases
            var resourceCenters = GetUnits(OwnedUnits, Units.ResourceCenters, onlyCompleted: true);
            Unit transferFrom = null;
            Unit transferTo = null;
            foreach (var rc in resourceCenters) {
                if (rc.AssignedWorkers <= rc.IdealWorkers) {
                    transferTo = rc;
                }
                else {
                    transferFrom = rc;
                }
            }

            if ((transferFrom != null) && (transferTo != null)) {
                var mineralFields = GetUnits(OwnedUnits, Units.MineralFields, onlyVisible: true).ToList();

                var sqrDistance = 7 * 7;
                foreach (var worker in workers) {
                    if (worker.Order.AbilityId != Abilities.GatherMinerals) {
                        continue;
                    }

                    if (Vector3.DistanceSquared(worker.Position, transferFrom.Position) > sqrDistance) {
                        continue;
                    }

                    var mf = GetFirstInRange(transferTo.Position, mineralFields, 7);
                    if (mf == null) {
                        continue;
                    }

                    //only one at a time
                    Logger.Info("Distributing idle worker: {0}", worker.Tag);
                    worker.Smart(mf);

                    return;
                }
            }
        }
    }

    // TODO GD Get rid?
    private static Unit GetAvailableWorker(Vector3 targetPosition) {
        var workers = GetUnits(OwnedUnits, Units.Workers);
        foreach (var worker in workers) {
            if (worker.Order.AbilityId != Abilities.GatherMinerals) {
                continue;
            }

            return worker;
        }

        return null;
    }

    // TODO GD Get rid?
    private static bool IsInRange(Vector3 targetPosition, List<Unit> units, float maxDistance) {
        return (GetFirstInRange(targetPosition, units, maxDistance) != null);
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
            Logger.Error("Unable to construct: {0}. No resource center was found.", GetUnitName(buildingType));

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
            throw new NotImplementedException($"Producer for unit {GetUnitName(unitOrAbilityType)} not found");
        }

        var possibleProducers = Units.Producers[unitOrAbilityType];

        return GetUnits(OwnedUnits, new HashSet<uint>(possibleProducers), onlyCompleted: true)
            .FirstOrDefault(unit => unit.Orders.Count(order => order.AbilityId != Abilities.DroneGather && order.AbilityId != Abilities.DroneReturnCargo) == 0);
    }

    public static bool ExecuteBuildStep(BuildOrders.BuildStep buildStep) {
        switch (buildStep.BuildType) {
            case BuildType.Train:
                return TrainUnit(buildStep.UnitOrAbilityType);
            case BuildType.Build:
                return PlaceBuilding(buildStep.UnitOrAbilityType);
            case BuildType.Research:
                return ResearchTech((int)buildStep.UnitOrAbilityType);
        }

        return false;
    }

    public static bool TrainUnit(uint unitType) {
        var producer = GetAvailableProducer(unitType);

        return TrainUnit(unitType, producer);
    }

    public static bool TrainUnit(uint unitType, Unit producer, bool queue = false)
    {
        if (producer == null || !CanAfford(unitType)) {
            return false;
        }

        if (!queue && producer.Orders.Count > 0) {
            return false;
        }

        producer.TrainUnit(unitType);

        var unitData = GameData.Units[(int)unitType];
        Minerals -= unitData.MineralCost * (unitType == Units.Zergling ? 2U : 1U);
        Vespene -= unitData.VespeneCost;

        return true;
    }

    public static bool PlaceBuilding(uint buildingType) {
        var producer = GetAvailableProducer(buildingType);

        return PlaceBuilding(buildingType, producer);
    }

    public static bool PlaceBuilding(uint buildingType, Unit producer) {
        if (producer == null || !CanAfford(buildingType, true)) {
            return false;
        }

        if (buildingType == Units.Extractor) {
            // TODO GD Exclude geysers with extractors on them
            // TODO GD Smarter choice of gas location
            var anyGas = GetUnits(NeutralUnits, Units.GasGeysers, onlyVisible: true).FirstOrDefault();
            if (anyGas == null) {
                return false;
            }

            producer.PlaceExtractor(buildingType, anyGas);
        }
        else {
            var constructionSpot = FindConstructionSpot(buildingType);

            producer.PlaceBuilding(buildingType, constructionSpot);
        }

        var buildingData = GameData.Units[(int)buildingType];
        Minerals -= buildingData.MineralCost;
        Vespene -= buildingData.VespeneCost;

        return true;
    }

    public static bool ResearchTech(int researchAbilityId) {
        var producer = GetAvailableProducer((uint)researchAbilityId);

        return ResearchTech(researchAbilityId, producer);
    }

    public static bool ResearchTech(int researchAbilityId, Unit producer) {
        if (producer == null || !CanAfford((uint)researchAbilityId)) {
            return false;
        }

        producer.ResearchTech(researchAbilityId);

        var unitData = GameData.Units[researchAbilityId];
        Minerals -= unitData.MineralCost;
        Vespene -= unitData.VespeneCost;

        return true;
    }

    public static IList<Unit> GetAvailableLarvae() {
        return GetUnits(OwnedUnits, Units.Larva, onlyCompleted: true).Where(larva => larva.Orders.Count == 0).ToList();
    }

    public static IEnumerable<Unit> GetUnits(IEnumerable<Unit> unitPool, uint unitToGet, bool onlyCompleted = false, bool onlyVisible = false) {
        return GetUnits(unitPool, new HashSet<uint>{ unitToGet }, onlyCompleted, onlyVisible);
    }

    public static IEnumerable<Unit> GetUnits(IEnumerable<Unit> unitPool, HashSet<uint> unitsToGet, bool onlyCompleted = false, bool onlyVisible = false) {
        foreach (var unit in unitPool) {
            if (unitsToGet.Contains(unit.UnitType)) {
                if (onlyCompleted && unit.BuildProgress < 1) {
                    continue;
                }

                if (onlyVisible && !unit.IsVisible) {
                    continue;
                }

                yield return unit;
            }
        }
    }

    public static bool CanAfford(uint unitType, bool isZergBuilding = false)
    {
        var unitData = GameData.Units[(int)unitType];

        var mineralCost = unitData.MineralCost;
        if (unitType == Units.Zergling) {
            // The unit data for Zerglings is for a single Zergling, even if they always spawn in pairs.
            mineralCost *= 2;
        }

        // TODO GD Make method to get the true mineral cost, not the unit's value
        if (isZergBuilding) {
            // The unit data for Zerg buildings includes the Drone cost
            mineralCost -= GameData.Units[(int)Units.Drone].MineralCost;
        }

        return (Minerals >= mineralCost) && (Vespene >= unitData.VespeneCost);
    }
}
