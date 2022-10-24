using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameSense;

namespace Bot.Managers.ScoutManagement.ScoutingTasks;

public class MaintainVisibilityScoutingTask : ScoutingTask {
    private const bool DrawEnabled = true;
    private bool _isCanceled = false;

    private readonly HashSet<Vector3> _areaToScout;

    public MaintainVisibilityScoutingTask(IReadOnlyCollection<Vector3> area, int priority, int maxScouts)
        : base(GetCenter(area), priority, maxScouts) {
        _areaToScout = area.ToHashSet();
        // TODO GD Pre-Compute or cache solo coverage
    }

    public override bool IsComplete() {
        return _isCanceled;
    }

    public override void Cancel() {
        _isCanceled = true;
    }

    public override void Execute(HashSet<Unit> scouts) {
        DebugAreaToScout();

        if (scouts.Count == 0) {
            return;
        }

        if (IsComplete()) {
            return;
        }

        if (IsCoverageGoodEnough()) {
            return;
        }

        if (scouts.Count == 1) {
            SoloScout(scouts.First());
        }
        else {
            TeamScout(scouts.ToList());
        }
    }

    /// <summary>
    /// Gets the center of an area
    /// </summary>
    /// <param name="area">The area to find the center of</param>
    /// <returns>A cell that's the center of the area</returns>
    private static Vector3 GetCenter(IReadOnlyCollection<Vector3> area) {
        if (area.Count <= 0) {
            Logger.Error("Trying to GetCenter of an empty area");

            return default;
        }

        var avgX = area.Average(element => element.X);
        var avgY = area.Average(element => element.Y);

        return new Vector3(avgX, avgY, 0).WithWorldHeight();
    }

    /// <summary>
    /// Scout the area with a single scout.
    /// The scout will attempt to cover as much area as possible.
    /// </summary>
    /// <param name="scout">The unit to scout with</param>
    private void SoloScout(Unit scout) {
        var coverageScores = ComputeCoverageScores(new List<Unit> { scout })[scout];
        var cellToExplore = _areaToScout.MinBy(cell => coverageScores[cell]);

        scout.Move(cellToExplore);
        Program.GraphicalDebugger.AddLink(scout.Position, cellToExplore, Colors.LightBlue, withText: false);
    }

    /// <summary>
    /// Scout the area with multiple scouts.
    /// The scouts will attempt to cover as much area as possible together.
    /// </summary>
    /// <param name="scouts">The units to scout with</param>
    private void TeamScout(List<Unit> scouts) {
        var distanceToScouts = new Dictionary<Vector3, Dictionary<Unit, float>>();
        foreach (var cellToScout in _areaToScout) {
            var distances = new Dictionary<Unit, float>();
            foreach (var scout in scouts) {
                distances[scout] = cellToScout.HorizontalDistanceTo(scout);
            }

            distanceToScouts[cellToScout] = distances;
        }

        var coverageScores = ComputeCoverageScores(scouts);
        foreach (var scout in scouts) {
            var scoutCoverageScores = coverageScores[scout];
            var cellToExplore = _areaToScout.MaxBy(cell => {
                var coverageScore = scoutCoverageScores[cell];

                var cellDistances = distanceToScouts[cell];
                var myDistance = cellDistances[scout];
                var minDistance = cellDistances.Min(kv => kv.Value);
                var maxDistance = cellDistances.Max(kv => kv.Value);

                var penalty = 1f;
                if (maxDistance > minDistance) {
                    penalty += (myDistance - minDistance) / (maxDistance - minDistance);
                }

                return coverageScore / penalty;
            });

            scout.Move(cellToExplore);
            Program.GraphicalDebugger.AddLink(scout.Position, cellToExplore, Colors.LightBlue, withText: false);
        }
    }

    private void DebugAreaToScout() {
        if (!DrawEnabled) {
            return;
        }

        foreach (var cell in _areaToScout) {
            var color = VisibilityTracker.IsVisible(cell) ? Colors.LightBlue : Colors.LightRed;
            Program.GraphicalDebugger.AddGridSquare(cell, color);
        }
    }

    /// <summary>
    /// For each scout, computes the vision coverage provided by a scout for each cell considering the vision already granted by the others.
    /// </summary>
    /// <param name="scouts">The scouts</param>
    /// <returns>The vision coverage of each scout for each cell</returns>
    private Dictionary<Unit, Dictionary<Vector3, float>> ComputeCoverageScores(List<Unit> scouts) {
        var scoutsVision = new Dictionary<Unit, List<Vector3>>();
        foreach (var scout in scouts) {
            scoutsVision[scout] = _areaToScout.Where(cell => scout.IsInSightRangeOf(cell)).ToList();
        }

        var coverageScores = new Dictionary<Unit, Dictionary<Vector3, float>>();
        foreach (var scout in scouts) {
            var otherScoutsCoverage = scoutsVision
                .Where(kv => kv.Key != scout)
                .SelectMany(kv => kv.Value)
                .ToHashSet();

            var uncoveredArea = _areaToScout
                .Where(cell => !otherScoutsCoverage.Contains(cell))
                .ToList();

            var coverageScore = new Dictionary<Vector3, float>();
            foreach (var cell in _areaToScout) {
                coverageScore[cell] = uncoveredArea.Count(cellToCover => cellToCover.HorizontalDistanceTo(cell) <= scout.UnitTypeData.SightRange);
            }

            coverageScores[scout] = coverageScore;
        }

        return coverageScores;
    }

    private bool IsCoverageGoodEnough() {
        var visibilityPercent = (float)_areaToScout.Count(VisibilityTracker.IsVisible) / _areaToScout.Count;

        return visibilityPercent > 0.95f;
    }
}
