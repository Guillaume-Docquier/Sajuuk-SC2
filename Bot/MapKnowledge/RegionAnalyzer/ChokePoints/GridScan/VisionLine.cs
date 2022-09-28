using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;

namespace Bot.MapKnowledge;

public static partial class GridScanChokeFinder {
    private class VisionLine {
        public List<Vector3> OrderedTraversedCells { get; }
        public int Angle { get; }

        public Vector3 Start { get; }
        public Vector3 End { get; }
        public float Length { get; }

        public VisionLine(Vector3 start, Vector3 end, int angle) {
            var centerOfStart = start.AsWorldGridCenter();

            OrderedTraversedCells = start.GetPointsInBetween(end)
                .OrderBy(current => current.HorizontalDistanceTo(centerOfStart))
                .ToList();

            Start = OrderedTraversedCells[0];
            End = OrderedTraversedCells.Last();
            Length = Start.HorizontalDistanceTo(End);

            Angle = angle;
        }

        public VisionLine(List<Vector3> orderedTraversedCells, int angle) {
            OrderedTraversedCells = orderedTraversedCells;

            Start = OrderedTraversedCells[0];
            End = OrderedTraversedCells.Last();
            Length = Start.HorizontalDistanceTo(End);

            Angle = angle;
        }
    }
}
