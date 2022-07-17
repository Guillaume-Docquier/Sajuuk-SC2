using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.GameData;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot;

public class BuildingTracker: IWatchUnitsDie {
    private readonly Dictionary<Vector3, Unit> _reservedBuildingCells = new Dictionary<Vector3, Unit>();
    private readonly Dictionary<Unit, List<Vector3>> _ongoingBuildingOrders = new Dictionary<Unit, List<Vector3>>();

    public void Update() {
        foreach (var reservedBuildingCell in _reservedBuildingCells.Keys) {
            GraphicalDebugger.AddSquare(reservedBuildingCell, 1, Colors.Yellow);
        }
    }

    public void ReportUnitDeath(Unit deadUnit) {
        if (!_ongoingBuildingOrders.ContainsKey(deadUnit)) {
            Logger.Error("Could not find builder {0} in the ongoingBuildingOrders: [{1}]", deadUnit.Tag, string.Join(", ", _ongoingBuildingOrders.Keys.Select(unit => unit.Tag)));
        }
        else {
            _ongoingBuildingOrders[deadUnit].ForEach(buildingCell => _reservedBuildingCells.Remove(buildingCell));
            _ongoingBuildingOrders.Remove(deadUnit);
        }
    }

    public Vector3 FindConstructionSpot(uint buildingType) {
        var startingSpot = Controller.StartingTownHall.Position;
        var searchGrid = MapAnalyzer.BuildSearchGrid(startingSpot, gridRadius: 12, stepSize: 2);
        var mineralFields = Controller.GetUnits(Controller.NeutralUnits, Units.MineralFields).ToList();

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

    public void ConfirmPlacement(uint buildingType, Vector3 position, Unit builder) {
        builder.AddDeathWatcher(this);

        var buildingCells = GetBuildingCells(buildingType, position).ToList();
        buildingCells.ForEach(buildingCell => _reservedBuildingCells[buildingCell] = builder);
        _ongoingBuildingOrders[builder] = buildingCells;
    }

    // This is a blocking call! Use it sparingly, or you will slow down your execution significantly!
    public bool CanPlace(uint buildingType, Vector3 position) {
        if (IsTooCloseToTownHall(buildingType, position)) {
            return false;
        }

        if (GetBuildingCells(buildingType, position).Any(buildingCell => _reservedBuildingCells.ContainsKey(buildingCell))) {
            return false;
        }

        var queryBuildingPlacement = new RequestQueryBuildingPlacement
        {
            AbilityId = (int)KnowledgeBase.GetUnitTypeData(buildingType).AbilityId,
            TargetPos = new Point2D
            {
                X = position.X,
                Y = position.Y
            }
        };

        var requestQuery = new Request
        {
            Query = new RequestQuery()
        };
        requestQuery.Query.Placements.Add(queryBuildingPlacement); // TODO GD Can I send multiple placements at the same time?

        var result = Program.GameConnection.SendQuery(requestQuery.Query);
        if (result.Result.Placements.Count == 0) {
            return false;
        }

        if (result.Result.Placements.Count > 1) {
            Logger.Warning("[CanPlace] Expected 1 placement, found {0}", result.Result.Placements.Count);
        }

        var actionResult = result.Result.Placements[0].Result;
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
            GraphicalDebugger.AddSquare(targetPos, 1f, Colors.Black);
        }
        else if (actionResult == ActionResult.CantBuildLocationInvalid) {
            GraphicalDebugger.AddSquare(targetPos, 1f, Colors.Red);
        }
        else if (actionResult == ActionResult.CantBuildTooCloseToResources) {
            GraphicalDebugger.AddSquare(targetPos, 1f, Colors.Cyan);
        }
        else if (actionResult == ActionResult.Success) {
            GraphicalDebugger.AddSquare(targetPos, 1f, Colors.Green);
        }
        else {
            Logger.Warning("[CanPlace] Unexpected placement result: {0}", actionResult);
            GraphicalDebugger.AddSquare(targetPos, 1f, Colors.Magenta);
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

    private bool IsTooCloseToTownHall(uint buildingType, Vector3 position) {
        var buildingDimension = Buildings.Dimensions[buildingType];
        var townHallDimension = Buildings.Dimensions[Units.Hatchery];

        // Leave at least 1 cell around town halls
        var minDistance = buildingDimension.Radius + townHallDimension.Radius + 1;

        return Controller.GetUnits(Controller.OwnedUnits, Units.Hatchery).Any(townHall => townHall.DistanceTo(position) <= minDistance);
    }
}
