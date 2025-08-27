using System.Numerics;
using Algorithms;
using Algorithms.ExtensionMethods;
using SC2Client.Trackers;

namespace MapAnalysis.RegionAnalysis.ChokePoints;

public class VisionLine : IHavePosition {
    private readonly ITerrainTracker _terrainTracker;

    public List<Vector2> OrderedTraversedCells { get; }
    public int Angle { get; }
    public Vector2 StartCell { get; }
    public Vector2 EndCell { get; }
    public Vector3 Position => Vector3.Lerp(_terrainTracker.WithWorldHeight(StartCell), _terrainTracker.WithWorldHeight(EndCell), 0.5f);
    public float Length { get; }

    public VisionLine(
        ITerrainTracker terrainTracker,
        Vector2 startPos,
        Vector2 endPos,
        int angle
    ) {
        _terrainTracker = terrainTracker;

        var startCellCenter = startPos.AsCellCenter();

        OrderedTraversedCells = RayCasting.RayCastPosToPos(startPos, endPos)
            .Select(rayCastResult => rayCastResult.Cell)
            .OrderBy(current => current.DistanceTo(startCellCenter))
            .ToList();

        StartCell = OrderedTraversedCells[0];
        EndCell = OrderedTraversedCells.Last();
        Length = StartCell.DistanceTo(EndCell);

        Angle = angle;
    }

    public VisionLine(
        ITerrainTracker terrainTracker,
        List<Vector2> orderedTraversedCells,
        int angle
    ) {
        _terrainTracker = terrainTracker;

        OrderedTraversedCells = orderedTraversedCells;

        StartCell = OrderedTraversedCells[0];
        EndCell = OrderedTraversedCells.Last();
        Length = StartCell.DistanceTo(EndCell);

        Angle = angle;
    }
}
