using System;
using System.Numerics;
using Sajuuk.ExtensionMethods;
using Sajuuk.GameData;
using Sajuuk.MapAnalysis;

namespace Sajuuk.Builds.BuildRequests.Fulfillment.Building;

public class PlaceBuildingTimeToTravelCalculator : ITimeToTravelCalculator {
    private readonly IPathfinder _pathfinder;
    private readonly Unit _producer;
    private readonly Vector2 _targetLocation;

    public PlaceBuildingTimeToTravelCalculator(
        IPathfinder pathfinder,
        Unit producer,
        Vector2 targetLocation
    ) {
        _pathfinder = pathfinder;
        _producer = producer;
        _targetLocation = targetLocation;
    }

    public uint CalculateTimeToTravel() {
        var pathDistance = _pathfinder.FindPath(_producer.Position.ToVector2(), _targetLocation).GetPathDistance();

        return (uint)Math.Round(pathDistance / _producer.Speed);
    }
}
