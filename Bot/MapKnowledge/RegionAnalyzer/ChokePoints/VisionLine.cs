using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Bot.ExtensionMethods;

namespace Bot.MapKnowledge;

public partial class RayCastingChokeFinder {
    public class VisionLine: IHavePosition {
        public List<Vector2> OrderedTraversedCells { get; }
        public int Angle { get; }

        public Vector2 Start { get; }
        public Vector2 End { get; }
        // TODO GD Get rid of MapAnalyzer.Instance, provide and serialize Position
        public Vector3 Position => Vector3.Lerp(MapAnalyzer.Instance.WithWorldHeight(Start), MapAnalyzer.Instance.WithWorldHeight(End), 0.5f);
        public float Length { get; }

        [JsonConstructor]
        public VisionLine(List<Vector2> orderedTraversedCells, int angle, Vector2 start, Vector2 end, float length) {
            OrderedTraversedCells = orderedTraversedCells;
            Angle = angle;

            Start = start;
            End = end;
            Length = length;
        }

        public VisionLine(Vector2 start, Vector2 end, int angle) {
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
