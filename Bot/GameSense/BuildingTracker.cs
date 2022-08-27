using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.GameData;
using Bot.MapKnowledge;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.GameSense;

public class BuildingTracker: INeedUpdating, IWatchUnitsDie {
    public static readonly BuildingTracker Instance = new BuildingTracker();

    private readonly Dictionary<Vector3, Unit> _reservedBuildingCells = new Dictionary<Vector3, Unit>();
    private readonly Dictionary<Unit, (uint buildingType, Vector3 position, List<Vector3> cells)> _ongoingBuildingOrders = new();

    private BuildingTracker() {}

    public void Update(ResponseObservation observation) {
        foreach (var reservedBuildingCell in _reservedBuildingCells.Keys) {
            GraphicalDebugger.AddGridSquare(reservedBuildingCell, Colors.Yellow);
        }

        foreach (var worker in _ongoingBuildingOrders.Keys) {
            var buildingOrder = _ongoingBuildingOrders[worker];
            if (!worker.IsProducing(buildingOrder.buildingType, atLocation: buildingOrder.position)) {
                ClearBuildingOrder(worker);
            }
        }
    }

    public void ReportUnitDeath(Unit deadUnit) {
        if (!_ongoingBuildingOrders.ContainsKey(deadUnit)) {
            Logger.Error("Could not find builder {0} in the ongoingBuildingOrders: [{1}]", deadUnit.Tag, string.Join(", ", _ongoingBuildingOrders.Keys.Select(unit => unit.Tag)));
        }
        else {
            ClearBuildingOrder(deadUnit);
        }
    }

    public static Vector3 FindConstructionSpot(uint buildingType) {
        var startingSpot = MapAnalyzer.StartingLocation;
        var searchGrid = MapAnalyzer.BuildSearchGrid(startingSpot, gridRadius: 12, stepSize: 2);
        var mineralFields = Controller.GetUnits(UnitsTracker.NeutralUnits, Units.MineralFields).ToList();

        foreach (var constructionCandidate in searchGrid) {
            // Avoid building in the mineral line
            if (IsInRange(constructionCandidate, mineralFields, 5)) {
                continue;
            }

            // Check if the building fits
            if (CanPlace(buildingType, constructionCandidate)) {
                return constructionCandidate;
            }
        }

        Logger.Error("Could not find a construction spot for {0}", KnowledgeBase.GetUnitTypeData(buildingType).Name);

        return default;
    }

    public static void ConfirmPlacement(uint buildingType, Vector3 position, Unit builder) {
        builder.AddDeathWatcher(Instance);

        if (Instance._ongoingBuildingOrders.ContainsKey(builder)) {
            Instance.ClearBuildingOrder(builder);
        }

        var buildingCells = GetBuildingCells(buildingType, position).ToList();
        buildingCells.ForEach(buildingCell => Instance._reservedBuildingCells[buildingCell] = builder);
        Instance._ongoingBuildingOrders[builder] = (buildingType, position, buildingCells);
    }

    private void ClearBuildingOrder(Unit unit) {
        _ongoingBuildingOrders[unit].cells.ForEach(buildingCell => _reservedBuildingCells.Remove(buildingCell));
        _ongoingBuildingOrders.Remove(unit);
    }

    // This is a blocking call! Use it sparingly, or you will slow down your execution significantly!
    public static bool CanPlace(uint buildingType, Vector3 position) {
        if (IsTooCloseToTownHall(buildingType, position)) {
            return false;
        }

        if (GetBuildingCells(buildingType, position).Any(buildingCell => Instance._reservedBuildingCells.ContainsKey(buildingCell))) {
            return false;
        }

        if (Units.Extractors.Contains(buildingType)) {
            // Extractors are placed on gas, let's not query the terrain
            return true;
        }

        // TODO GD Check with MapAnalyzer._currentWalkMap before checking with the query
        var queryBuildingPlacementResponse = Program.GameConnection.SendRequest(RequestBuilder.RequestQueryBuildingPlacement(buildingType, position)).Result;
        if (queryBuildingPlacementResponse.Query.Placements.Count == 0) {
            return false;
        }

        if (queryBuildingPlacementResponse.Query.Placements.Count > 1) {
            Logger.Warning("[CanPlace] Expected 1 placement, found {0}", queryBuildingPlacementResponse.Query.Placements.Count);
        }

        var actionResult = queryBuildingPlacementResponse.Query.Placements[0].Result;
        DebugBuildingPlacementResult(actionResult, position);

        return actionResult == ActionResult.Success;
    }

    private static bool IsInRange(Vector3 targetPosition, List<Unit> units, float maxDistance) {
        return GetFirstInRange(targetPosition, units, maxDistance) != null;
    }

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

    private static void DebugBuildingPlacementResult(ActionResult actionResult, Vector3 targetPos) {
        if (actionResult == ActionResult.NotSupported) {
            GraphicalDebugger.AddGridSquare(targetPos, Colors.Black);
        }
        else if (actionResult == ActionResult.CantBuildLocationInvalid) {
            GraphicalDebugger.AddGridSquare(targetPos, Colors.Red);
        }
        else if (actionResult == ActionResult.CantBuildTooCloseToResources) {
            GraphicalDebugger.AddGridSquare(targetPos, Colors.Cyan);
        }
        else if (actionResult == ActionResult.Success) {
            GraphicalDebugger.AddGridSquare(targetPos, Colors.Green);
        }
        else {
            Logger.Warning("[CanPlace] Unexpected placement result: {0}", actionResult);
            GraphicalDebugger.AddGridSquare(targetPos, Colors.Magenta);
        }
    }

    private static IEnumerable<Vector3> GetBuildingCells(uint buildingType, Vector3 position) {
        var buildingDimension = Buildings.Dimensions[buildingType];

        // Odd dimensions are centered
        // Even depends on the quadrant in the cell that you are aiming at
        // Up left would make the target cell the bottom right corner
        // Up right would make the target cell the bottom left corner
        // Down left would make the target cell the top right corner
        // Down right would make the target cell the top left corner
        //
        // But our code should aim at exactly the center, so not sure where it's gonna be
        // Let's assume that it's bigger than it is?

        var deltaX = Convert.ToInt32(Math.Ceiling((double)(buildingDimension.Width - 1) / 2));
        var deltaY = Convert.ToInt32(Math.Ceiling((double)(buildingDimension.Height - 1) / 2));

        for (var x = position.X - deltaX; x <= position.X + deltaX; x++) {
            for (var y = position.Y - deltaY; y <= position.Y + deltaY; y++) {
                yield return new Vector3 { X = x, Y = y, Z = position.Z };
            }
        }
    }

    private static bool IsTooCloseToTownHall(uint buildingType, Vector3 position) {
        var buildingDimension = Buildings.Dimensions[buildingType];
        var townHallDimension = Buildings.Dimensions[Units.Hatchery];

        // Leave at least 1 cell around town halls
        var minDistance = buildingDimension.Radius + townHallDimension.Radius + 1;

        return Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery).Any(townHall => townHall.DistanceTo(position) <= minDistance);
    }
}
