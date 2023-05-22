using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameSense;

namespace Bot.MapAnalysis.RegionAnalysis.ChokePoints;

public partial class RayCastingChokeFinder {
    public class VisionLine: IHavePosition {
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

        public VisionLine(List<Vector2> orderedTraversedCells, int angle) {
            OrderedTraversedCells = orderedTraversedCells;

            Start = OrderedTraversedCells[0];
            End = OrderedTraversedCells.Last();
            Length = Start.DistanceTo(End);

            Angle = angle;
        }
    }
}
