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

        public float ChokeScore { get; private set; }
        public List<VisionLine> MostLikelyChokeLines { get; private set; }

        public Node(Vector3 position) {
            Position = position;
        }

        public void UpdateChokeScore() {
            var chokeLinesWithScores = VisionLines
                .Select(line => (Line: line, Score: Scorers.MinOfBothHalvesSquaredLineScore(this, line)))
                .OrderByDescending(group => group.Score)
                .Take(VisionLines.Count / 4)
                .ToList();

            MostLikelyChokeLines = chokeLinesWithScores.Select(group => group.Line).ToList();

            ChokeScore = chokeLinesWithScores.Average(group => group.Score);
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

            return Math.Min(startChokeScore, endChokeScore) * 2;
        }

        public static float MinOfBothHalvesSquaredLineScore(Node node, VisionLine visionLine) {
            return (float)Math.Pow(MinOfBothHalvesLineScore(node, visionLine), 2);
        }
    }
}
