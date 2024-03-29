﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Sajuuk.ExtensionMethods;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.GameSense;
using Sajuuk.Utils;

namespace Sajuuk.Managers.ScoutManagement.ScoutingTasks;

public class MaintainVisibilityScoutingTask : ScoutingTask {
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IFrameClock _frameClock;

    private const bool DrawEnabled = false;

    private const int ResolutionReductionFactor = 2;
    private static readonly double ExecuteEvery = TimeUtils.SecsToFrames(1);

    private bool _isCanceled = false;

    private readonly HashSet<Vector2> _areaToScout;

    public MaintainVisibilityScoutingTask(
        IVisibilityTracker visibilityTracker,
        ITerrainTracker terrainTracker,
        IGraphicalDebugger graphicalDebugger,
        IFrameClock frameClock,
        IReadOnlyCollection<Vector2> area,
        int priority,
        int maxScouts
    ) : base(GetCenter(area), priority, maxScouts) {
        _visibilityTracker = visibilityTracker;
        _terrainTracker = terrainTracker;
        _graphicalDebugger = graphicalDebugger;
        _frameClock = frameClock;

        // Lower the resolution for better time performance on large areas
        // The algorithm results are virtually unaffected by this
        _areaToScout = area
            .Where(cell => (cell.X + cell.Y) % ResolutionReductionFactor == 0)
            .ToHashSet();
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

        // For performance reasons, execute scarcely
        if (_frameClock.CurrentFrame % ExecuteEvery != 0) {
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
    private static Vector2 GetCenter(IReadOnlyCollection<Vector2> area) {
        if (area.Count <= 0) {
            Logger.Error("Trying to GetCenter of an empty area");

            return default;
        }

        var avgX = area.Average(element => element.X);
        var avgY = area.Average(element => element.Y);

        return new Vector2(avgX, avgY);
    }

    /// <summary>
    /// Scout the area with a single scout.
    /// The scout will attempt to cover as much area as possible.
    /// </summary>
    /// <param name="scout">The unit to scout with</param>
    private void SoloScout(Unit scout) {
        var coverageScores = ComputeCoverageScores(new List<Unit> { scout })[scout];
        var cellToExplore = _areaToScout.MaxBy(cell => coverageScores[cell]);

        scout.Move(cellToExplore);
        _graphicalDebugger.AddLink(scout.Position, _terrainTracker.WithWorldHeight(cellToExplore), Colors.LightBlue);
    }

    /// <summary>
    /// Scout the area with multiple scouts.
    /// The scouts will attempt to cover as much area as possible together.
    /// </summary>
    /// <param name="scouts">The units to scout with</param>
    private void TeamScout(List<Unit> scouts) {
        var distanceToScouts = new Dictionary<Vector2, Dictionary<Unit, float>>();
        foreach (var cellToScout in _areaToScout) {
            var distances = new Dictionary<Unit, float>();
            foreach (var scout in scouts) {
                distances[scout] = cellToScout.DistanceTo(scout);
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
            _graphicalDebugger.AddLink(scout.Position, _terrainTracker.WithWorldHeight(cellToExplore), Colors.LightBlue);
        }
    }

    private void DebugAreaToScout() {
        if (!DrawEnabled) {
            return;
        }

        foreach (var cell in _areaToScout) {
            var color = _visibilityTracker.IsVisible(cell) ? Colors.LightBlue : Colors.LightRed;
            _graphicalDebugger.AddGridSquare(_terrainTracker.WithWorldHeight(cell), color);
        }
    }

    /// <summary>
    /// For each scout, computes the vision coverage provided by a scout for each cell considering the vision already granted by the others.
    /// </summary>
    /// <param name="scouts">The scouts</param>
    /// <returns>The vision coverage of each scout for each cell</returns>
    private Dictionary<Unit, IDictionary<Vector2, float>> ComputeCoverageScores(List<Unit> scouts) {
        var scoutsVision = new Dictionary<Unit, List<Vector2>>();
        foreach (var scout in scouts) {
            scoutsVision[scout] = _areaToScout.Where(cell => scout.IsInSightRangeOf(cell)).ToList();
        }

        var coverageScores = new Dictionary<Unit, IDictionary<Vector2, float>>();
        foreach (var scout in scouts) {
            var otherScoutsCoverage = scoutsVision
                .Where(kv => kv.Key != scout)
                .SelectMany(kv => kv.Value)
                .ToHashSet();

            var uncoveredArea = _areaToScout
                .Where(cell => !otherScoutsCoverage.Contains(cell))
                .ToHashSet();

            // This loop is 99% of the computation time of the task
            // Without the Parallel.ForEach, this takes ~10ms for 600 cells
            // With it, it goes down to ~3ms
            var sightRangeSquared = scout.UnitTypeData.SightRange * scout.UnitTypeData.SightRange;
            var coverageScore = new ConcurrentDictionary<Vector2, float>();
            Parallel.ForEach(_areaToScout, cell => {
                // .DistanceSquared() is faster than .Distance()
                coverageScore[cell] = uncoveredArea.Count(cellToCover => Vector2.DistanceSquared(cellToCover, cell) <= sightRangeSquared);
            });

            coverageScores[scout] = coverageScore;
        }

        return coverageScores;
    }

    private bool IsCoverageGoodEnough() {
        return _areaToScout.All(_visibilityTracker.IsVisible);
    }
}
