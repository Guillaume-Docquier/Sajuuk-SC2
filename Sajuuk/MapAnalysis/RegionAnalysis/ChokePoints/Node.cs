using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sajuuk.ExtensionMethods;

namespace Sajuuk.MapAnalysis.RegionAnalysis.ChokePoints;

public partial class RayCastingChokeFinder {
    private class Node : IHavePosition {
        // We would like a Vector2 here but IHavePosition requires Vector3
        public Vector3 Position { get; }
        public List<VisionLine> VisionLines { get; } = new List<VisionLine>();

        public float ChokeScore { get; private set; }
        public List<VisionLine> MostLikelyChokeLines { get; private set; }

        public Node(Vector2 position) {
            Position = new Vector3 { X = position.X, Y = position.Y, Z = 0 };
        }

        /// <summary>
        /// The choke score is the average score of the top 25% choke lines
        /// </summary>
        public void UpdateChokeScore() {
            var bestChokeLinesWithTheirScores = VisionLines
                .Select(line => (Line: line, Score: Scorers.MinOfBothHalvesSquaredLineScore(this, line)))
                .OrderByDescending(group => group.Score)
                .Take(VisionLines.Count / 4)
                .ToList();

            MostLikelyChokeLines = bestChokeLinesWithTheirScores.Select(group => group.Line).ToList();
            ChokeScore = bestChokeLinesWithTheirScores.Average(group => group.Score);
        }
    }

    private static class Scorers {
        /// <summary>
        /// A good potentialChokeLine should be short and have long perpendicular rays.
        /// Both perpendicular rays should ideally be long.
        /// Computes a score assuming both perpendicular rays have the length of the shortest one.
        /// The score squared is maxed at 75.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="potentialChokeLine">A vision line to consider as a choke line</param>
        /// <returns></returns>
        public static float MinOfBothHalvesLineScore(Node node, VisionLine potentialChokeLine) {
            // Possible angles are [0, 175]
            var perpendicularLineAngle = (potentialChokeLine.Angle + 90) % 180;
            var perpendicularLine = node.VisionLines.Find(otherLine => otherLine.Angle == perpendicularLineAngle)!;
            var shortestHalfDistance = Math.Min(perpendicularLine.Start.DistanceTo(node.Position.ToVector2()), perpendicularLine.End.DistanceTo(node.Position.ToVector2()));

            // We set a max vision distance to avoid super high score on certain lines that can view very very far
            const float maxVisionDistance = 15f;
            var clampedHalfDistance = Math.Min(maxVisionDistance, shortestHalfDistance);

            // Not sure why we add 1 to the numerator
            // Add 1 to the denominator to avoid division by 0 (Should we just return 0 instead?)
            // Double the score as if we scored each half with the lowest score
            var score = (clampedHalfDistance + 1) / (potentialChokeLine.Length + 1) * 2;

            // Make the maximum score squared equal to 75
            // Scores beyond this have no real value and they skew other stats
            var maxScore = (float)Math.Sqrt(75);

            return Math.Min(maxScore, score);
        }

        /// <summary>
        /// Squares the MinOfBothHalves score.
        /// This makes small scores smaller, and big scores bigger making it easier to segregate them.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="visionLine"></param>
        /// <returns></returns>
        public static float MinOfBothHalvesSquaredLineScore(Node node, VisionLine visionLine) {
            return (float)Math.Pow(MinOfBothHalvesLineScore(node, visionLine), 2);
        }
    }
}
