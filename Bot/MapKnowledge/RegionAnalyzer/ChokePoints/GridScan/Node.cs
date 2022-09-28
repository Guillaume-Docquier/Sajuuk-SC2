using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;

namespace Bot.MapKnowledge;

public static partial class GridScanChokeFinder {
    private class Node : IHavePosition {
        private const float MaxVisionDistance = 15f;

        public Vector3 Position { get; }
        public List<VisionLine> VisionLines { get; } = new List<VisionLine>();

        public VisionLine ShortestVisionLine { get; private set; }
        public VisionLine BestChokeVisionLine { get; private set; }
        public float ChokeScore { get; private set; }

        public Node(Vector3 position) {
            Position = position;
        }

        public void UpdateShortestLine() {
            ShortestVisionLine = VisionLines.MinBy(line => line.Length);
        }

        public void UpdateBestChokeLine() {
            VisionLine bestChokeVisionLine = null;
            var bestChokeScore = 0f;

            foreach (var line in VisionLines) {
                var perpendicularLineAngle = (line.Angle + 90) % (MaxAngle + AngleIncrement);
                var perpendicularLine = VisionLines.Find(otherLine => otherLine.Angle == perpendicularLineAngle)!;

                var startDistance = Math.Min(MaxVisionDistance, perpendicularLine.Start.HorizontalDistanceTo(Position));
                var endDistance = Math.Min(MaxVisionDistance, perpendicularLine.End.HorizontalDistanceTo(Position));

                var startChokeScore = (startDistance + 1) / (line.Length + 1);
                var endChokeScore = (endDistance + 1) / (line.Length + 1);

                var chokeScore = Math.Min(startChokeScore, endChokeScore);
                if (chokeScore > bestChokeScore) {
                    bestChokeVisionLine = line;
                    bestChokeScore = chokeScore;
                }
            }

            BestChokeVisionLine = bestChokeVisionLine;
            ChokeScore = bestChokeScore;
        }
    }
}
