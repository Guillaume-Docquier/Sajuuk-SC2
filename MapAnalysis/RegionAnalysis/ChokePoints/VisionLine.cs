using System.Numerics;
using Algorithms;
using Algorithms.ExtensionMethods;
using SC2Client.ExtensionMethods;
using SC2Client.Trackers;

namespace MapAnalysis.RegionAnalysis.ChokePoints;

public class VisionLine : IHavePosition {
    private readonly ITerrainTracker _terrainTracker;

    public List<Vector2> OrderedTraversedCells { get; }
    public int Angle { get; }
    public Vector2 Start { get; }
    public Vector2 End { get; }
    public Vector3 Position => Vector3.Lerp(_terrainTracker.WithWorldHeight(Start), _terrainTracker.WithWorldHeight(End), 0.5f);
    public float Length { get; }

    public VisionLine(
        ITerrainTracker terrainTracker,
        Vector2 start,
        Vector2 end,
        int angle
    ) {
        _terrainTracker = terrainTracker;

        var centerOfStart = start.AsWorldGridCenter();

        OrderedTraversedCells = start.GetPointsInBetween(end)
            .OrderBy(current => current.DistanceTo(centerOfStart))
            .ToList();

        Start = OrderedTraversedCells[0];
        End = OrderedTraversedCells.Last();
        Length = Start.DistanceTo(End);

        Angle = angle;
    }

    public VisionLine(
        ITerrainTracker terrainTracker,
        List<Vector2> orderedTraversedCells,
        int angle
    ) {
        _terrainTracker = terrainTracker;

        OrderedTraversedCells = orderedTraversedCells;

        Start = OrderedTraversedCells[0];
        End = OrderedTraversedCells.Last();
        Length = Start.DistanceTo(End);

        Angle = angle;
    }
}
