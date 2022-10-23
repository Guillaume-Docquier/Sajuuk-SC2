﻿using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.MapKnowledge;

namespace Bot.Managers.ScoutManagement.ScoutingTasks;

public class RegionScoutingTask : ScoutingTask {
    private readonly Region _region;
    private readonly HashSet<Vector3> _cellsToExplore;
    private bool _isCancelled = false;

    public RegionScoutingTask(Vector3 scoutLocation, int priority, int maxScouts)
        : base(scoutLocation, priority, maxScouts) {
        _region = scoutLocation.GetRegion();
        _cellsToExplore = new HashSet<Vector3>(_region.Cells);
    }

    public override bool IsComplete() {
        if (_isCancelled) {
            return true;
        }

        // Allow 7% unexplored to speed up, don't need to scout every single cell
        return (float)_cellsToExplore.Count / _region.Cells.Count <= 0.07;
    }

    public override void Cancel() {
        // Potentially do some cleanup / cancel sequence
        _isCancelled = true;
    }

    public override void Execute(HashSet<Unit> scouts) {
        UpdateCellsToExplore();

        if (scouts.Count == 0) {
            return;
        }

        if (IsComplete()) {
            return;
        }

        if (scouts.Count == 1) {
            SoloScout(scouts.First());
        }
        else {
            TeamScout(scouts);
        }
    }

    private void UpdateCellsToExplore() {
        foreach (var cellToExplore in _cellsToExplore.Where(VisibilityTracker.IsVisible).ToList()) {
            _cellsToExplore.Remove(cellToExplore);
        }
    }

    // TODO GD Consider keeping distance with walls to maximize vision
    private void SoloScout(Unit scout) {
        scout.Move(_cellsToExplore.MinBy(cell => cell.HorizontalDistanceTo(scout)));
    }

    // TODO GD Consider keeping distance with walls to maximize vision
    private void TeamScout(HashSet<Unit> scouts) {
        var distanceToScouts = new Dictionary<Vector3, Dictionary<Unit, float>>();
        foreach (var cellToExplore in _cellsToExplore) {
            var distances = new Dictionary<Unit, float>();
            foreach (var scout in scouts) {
                distances[scout] = cellToExplore.HorizontalDistanceTo(scout);
            }

            distanceToScouts[cellToExplore] = distances;
        }

        foreach (var scout in scouts) {
            var cellToExplore = _cellsToExplore.MinBy(cell => {
                var cellDistances = distanceToScouts[cell];
                var myDistance = cellDistances[scout];
                var minDistance = cellDistances.Min(kv => kv.Value);
                var maxDistance = cellDistances.Max(kv => kv.Value);

                var penalty = (myDistance - minDistance) / (maxDistance - minDistance);

                return myDistance + myDistance * penalty * 5;
            });

            scout.Move(cellToExplore);
        }
    }
}
