using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;

namespace Bot.MapKnowledge;

public static partial class GridScanChokeFinder {
    private class Node : IHavePosition {
        public Vector3 Position { get; }
        public List<VisionLine> VisionLines { get; } = new List<VisionLine>();

        public VisionLine ShortestVisionLine { get; private set; }
        public VisionLine BestChokeVisionLine { get; private set; }
        public float ChokeScore { get; private set; }
        public float ChokeScoreDelta { get; private set; }

        public Node(Vector3 position) {
            Position = position;
        }

        public void UpdateShortestLine() {
            ShortestVisionLine = VisionLines.MinBy(line => line.Length);
        }

        public void UpdateChokeScore() {
            VisionLine bestChokeVisionLine = null;
            var bestChokeScore = 0f;

            foreach (var line in VisionLines) {
                var chokeScore = Scorers.MinOfBothHalvesSquaredLineScore(this, line);
                if (chokeScore > bestChokeScore) {
                    bestChokeVisionLine = line;
                    bestChokeScore = chokeScore;
                }
            }

            BestChokeVisionLine = bestChokeVisionLine;
            ChokeScore = bestChokeScore;
        }

        public void UpdateChokeScoreDeltas(IEnumerable<Node> neighbors) {
            ChokeScoreDelta = neighbors.Max(neighbor => Math.Abs(neighbor.ChokeScore - ChokeScore));
        }
    }

    private static class Scorers {
        public static float MinOfBothHalvesLineScore(Node node, VisionLine visionLine) {
            const float maxVisionDistance = 15f;

            var perpendicularLineAngle = (visionLine.Angle + 90) % (MaxAngle + AngleIncrement);
            var perpendicularLine = node.VisionLines.Find(otherLine => otherLine.Angle == perpendicularLineAngle)!;

            var startDistance = Math.Min(maxVisionDistance, perpendicularLine.Start.HorizontalDistanceTo(node.Position));
            var endDistance = Math.Min(maxVisionDistance, perpendicularLine.End.HorizontalDistanceTo(node.Position));

            var startChokeScore = (startDistance + 1) / (visionLine.Length + 1);
            var endChokeScore = (endDistance + 1) / (visionLine.Length + 1);

            return Math.Min(startChokeScore, endChokeScore);
        }

        public static float MinOfBothHalvesSquaredLineScore(Node node, VisionLine visionLine) {
            return (float)Math.Pow(MinOfBothHalvesLineScore(node, visionLine), 2);
        }
    }
}
