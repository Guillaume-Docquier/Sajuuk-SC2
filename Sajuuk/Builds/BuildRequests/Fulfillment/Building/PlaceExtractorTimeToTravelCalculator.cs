using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sajuuk.ExtensionMethods;
using Sajuuk.GameSense;
using Sajuuk.MapAnalysis;

namespace Sajuuk.Builds.BuildRequests.Fulfillment.Building;

public class PlaceExtractorTimeToTravelCalculator : ITimeToTravelCalculator {
    private readonly IPathfinder _pathfinder;
    private readonly Unit _producer;

    private readonly List<Vector2> _targetLocations;

    public PlaceExtractorTimeToTravelCalculator(
        IPathfinder pathfinder,
        FootprintCalculator footprintCalculator,
        ITerrainTracker terrainTracker,
        Unit producer,
        Unit targetGasGeyser
    ) {
        _pathfinder = pathfinder;
        _producer = producer;

        // Gas geysers are not walkable, building starts when we reach their edge, not their center.
        _targetLocations = footprintCalculator.GetFootprint(targetGasGeyser)
            .SelectMany(cell => cell.GetNeighbors())
            .ToHashSet()
            .Where(cell => terrainTracker.IsWalkable(cell))
            .ToList();
    }

    public uint CalculateTimeToTravel() {
        var producerLocation = _producer.Position.ToVector2();
        var pathDistance = _targetLocations
            .OrderBy(targetLocation => targetLocation.DistanceTo(producerLocation))
            .Select(targetLocation => _pathfinder.FindPath(producerLocation, targetLocation))
            .First(path => path != null)
            .GetPathDistance();

        return (uint)Math.Round(pathDistance / _producer.Speed);
    }
}
