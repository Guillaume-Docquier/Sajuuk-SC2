using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.GameSense;

public class BuildingTracker : IBuildingTracker, INeedUpdating, IWatchUnitsDie {
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly KnowledgeBase _knowledgeBase;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IRequestBuilder _requestBuilder;
    private readonly ISc2Client _sc2Client;

    private readonly Dictionary<Vector2, Unit> _reservedBuildingCells = new Dictionary<Vector2, Unit>();
    private readonly Dictionary<Unit, (uint buildingType, Vector2 position, List<Vector2> cells)> _ongoingBuildingOrders = new();

    public BuildingTracker(
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        KnowledgeBase knowledgeBase,
        IGraphicalDebugger graphicalDebugger,
        IRequestBuilder requestBuilder,
        ISc2Client sc2Client
    ) {
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _knowledgeBase = knowledgeBase;
        _graphicalDebugger = graphicalDebugger;
        _requestBuilder = requestBuilder;
        _sc2Client = sc2Client;
    }

    public void Update(ResponseObservation observation, ResponseGameInfo gameInfo) {
        foreach (var reservedBuildingCell in _reservedBuildingCells.Keys) {
            _graphicalDebugger.AddGridSquare(_terrainTracker.WithWorldHeight(reservedBuildingCell), Colors.Yellow);
        }

        foreach (var worker in _ongoingBuildingOrders.Keys) {
            var buildingOrder = _ongoingBuildingOrders[worker];
            if (!worker.IsProducing(buildingOrder.buildingType, atLocation: buildingOrder.position)) {
                Logger.Warning("Worker {0} stopped its building assignment", worker);
                ClearBuildingOrder(worker);
            }

            if (worker.Manager != null) {
                Logger.Warning("Worker {0} is managed while building, but shouldn't be", worker);
                worker.Manager.Release(worker);
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

    public Vector2 FindConstructionSpot(uint buildingType) {
        var startingSpot = _terrainTracker.StartingLocation;
        var searchGrid = _terrainTracker.BuildSearchGrid(startingSpot, gridRadius: 12, stepSize: 2);
        var mineralFields = _unitsTracker.GetUnits(_unitsTracker.NeutralUnits, Units.MineralFields).ToList();

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

        Logger.Error("Could not find a construction spot for {0}", _knowledgeBase.GetUnitTypeData(buildingType).Name);

        return default;
    }

    public void ConfirmPlacement(uint buildingType, Vector2 position, Unit builder) {
        builder.AddDeathWatcher(this);

        if (_ongoingBuildingOrders.ContainsKey(builder)) {
            ClearBuildingOrder(builder);
        }

        var buildingCells = GetBuildingCells(buildingType, position).ToList();
        buildingCells.ForEach(buildingCell => _reservedBuildingCells[buildingCell] = builder);
        _ongoingBuildingOrders[builder] = (buildingType, position, buildingCells);
    }

    private void ClearBuildingOrder(Unit unit) {
        _ongoingBuildingOrders[unit].cells.ForEach(buildingCell => _reservedBuildingCells.Remove(buildingCell));
        _ongoingBuildingOrders.Remove(unit);
    }

    public bool CanPlace(uint buildingType, Vector2 position) {
        return QueryPlacement(buildingType, position) == ActionResult.Success;
    }

    // This is a blocking call! Use it sparingly, or you will slow down your execution significantly!
    public ActionResult QueryPlacement(uint buildingType, Vector2 position) {
        if (IsTooCloseToTownHall(buildingType, position)) {
            return ActionResult.CantBuildLocationInvalid;
        }

        if (GetBuildingCells(buildingType, position).Any(buildingCell => _reservedBuildingCells.ContainsKey(buildingCell))) {
            return ActionResult.CantBuildLocationInvalid;
        }

        if (Units.Extractors.Contains(buildingType)) {
            // Extractors are placed on gas, let's not query the terrain
            return ActionResult.Success;
        }

        // TODO GD Check with MapAnalyzer._currentWalkMap before checking with the query
        var queryBuildingPlacementResponse = _sc2Client.SendRequest(_requestBuilder.RequestQueryBuildingPlacement(buildingType, position)).Result;
        if (queryBuildingPlacementResponse.Query.Placements.Count == 0) {
            return ActionResult.NotSupported;
        }

        if (queryBuildingPlacementResponse.Query.Placements.Count > 1) {
            Logger.Warning("[CanPlace] Expected 1 placement, found {0}", queryBuildingPlacementResponse.Query.Placements.Count);
        }

        var actionResult = queryBuildingPlacementResponse.Query.Placements[0].Result;
        DebugBuildingPlacementResult(actionResult, _terrainTracker.WithWorldHeight(position));

        return actionResult;
    }

    private bool IsInRange(Vector2 targetPosition, List<Unit> units, float maxDistance) {
        return GetFirstInRange(targetPosition, units, maxDistance) != null;
    }

    private Unit GetFirstInRange(Vector2 targetPosition, List<Unit> units, float maxDistance) {
        //squared distance is faster to calculate
        var maxDistanceSqr = maxDistance * maxDistance;
        foreach (var unit in units) {
            if (Vector2.DistanceSquared(targetPosition, unit.Position.ToVector2()) <= maxDistanceSqr) {
                return unit;
            }
        }

        return null;
    }

    private void DebugBuildingPlacementResult(ActionResult actionResult, Vector3 targetPos) {
        if (actionResult == ActionResult.NotSupported) {
            _graphicalDebugger.AddGridSquare(targetPos, Colors.Black);
        }
        else if (actionResult == ActionResult.CantBuildLocationInvalid) {
            _graphicalDebugger.AddGridSquare(targetPos, Colors.Red);
        }
        else if (actionResult == ActionResult.CantBuildTooCloseToResources) {
            _graphicalDebugger.AddGridSquare(targetPos, Colors.Cyan);
        }
        else if (actionResult == ActionResult.Success) {
            _graphicalDebugger.AddGridSquare(targetPos, Colors.Green);
        }
        else {
            Logger.Warning("[CanPlace] Unexpected placement result: {0}", actionResult);
            _graphicalDebugger.AddGridSquare(targetPos, Colors.Magenta);
        }
    }

    private IEnumerable<Vector2> GetBuildingCells(uint buildingType, Vector2 position) {
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
                yield return new Vector2 { X = x, Y = y };
            }
        }
    }

    private bool IsTooCloseToTownHall(uint buildingType, Vector2 position) {
        var buildingDimension = Buildings.Dimensions[buildingType];
        var townHallDimension = Buildings.Dimensions[Units.Hatchery];

        // Leave at least 1 cell around town halls
        var minDistance = buildingDimension.Radius + townHallDimension.Radius + 1;

        return _unitsTracker.GetUnits(_unitsTracker.OwnedUnits, Units.Hatchery).Any(townHall => townHall.DistanceTo(position) <= minDistance);
    }
}
