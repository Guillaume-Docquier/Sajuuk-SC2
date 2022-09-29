using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Bot.ExtensionMethods;

namespace Bot.MapKnowledge;

public static partial class GridScanChokeFinder {
    private class VisionLine {
        public List<Vector3> OrderedTraversedCells { get; }
        public int Angle { get; }

        public Vector3 Start { get; }
        public Vector3 End { get; }
        public Vector3 Center => Vector3.Lerp(Start, End, 0.5f);
        public float Length { get; }

        [JsonConstructor]
        public VisionLine(List<Vector3> orderedTraversedCells, int angle, Vector3 start, Vector3 end, float length) {
            OrderedTraversedCells = orderedTraversedCells;
            Angle = angle;

            Start = start;
            End = end;
            Length = length;
        }

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
