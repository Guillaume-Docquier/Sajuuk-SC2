using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameSense;

namespace Bot.Managers.ScoutManagement.ScoutingTasks;

public class MaintainVisibilityScoutingTask : ScoutingTask {
    private const bool DrawEnabled = false;
    private bool _isCanceled = false;

    private readonly HashSet<Vector3> _areaToScout;
    private readonly Dictionary<Vector3, float> _edgenessScore = new Dictionary<Vector3, float>();

    public MaintainVisibilityScoutingTask(IReadOnlyCollection<Vector3> area, int priority, int maxScouts)
        : base(GetCenter(area), priority, maxScouts) {
        _areaToScout = area.ToHashSet();

        foreach (var cell in area) {
            _edgenessScore[cell] = area.Average(otherCell => otherCell.HorizontalDistanceTo(cell));
        }
    }

    public override bool IsComplete() {
        return _isCanceled;
    }

    public override void Cancel() {
        _isCanceled = true;
    }

    public override void Execute(HashSet<Unit> scouts) {
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

    private static Vector3 GetCenter(IReadOnlyCollection<Vector3> area) {
        if (area.Count <= 0) {
            Logger.Error("Trying to GetCenter of an empty area");

            return default;
        }

        var avgX = area.Average(element => element.X);
        var avgY = area.Average(element => element.Y);

        return new Vector3(avgX, avgY, 0).WithWorldHeight();
    }

    private void SoloScout(Unit scout) {
        var cellToExplore = _areaToScout.MinBy(cell => _edgenessScore[cell]);

        scout.Move(cellToExplore);
        Program.GraphicalDebugger.AddLink(scout.Position, cellToExplore, Colors.LightBlue, withText: false);
        DebugAreaToScout();
    }

    private void TeamScout(HashSet<Unit> scouts) {
        Controller.SetRealTime();

        var visibilityPercent = (float)_areaToScout.Count(VisibilityTracker.IsVisible) / _areaToScout.Count;
        if (visibilityPercent > 0.95f) {
            return;
        }

        var distanceToScouts = new Dictionary<Vector3, Dictionary<Unit, float>>();
        var densityScores = new Dictionary<Vector3, Dictionary<Unit, float>>();
        foreach (var cellToScout in _areaToScout) {
            var distances = new Dictionary<Unit, float>();
            foreach (var scout in scouts) {
                distances[scout] = cellToScout.HorizontalDistanceTo(scout);
            }

            var density = new Dictionary<Unit, float>();
            var maxDensityDistance = scouts.Max(scout => scout.UnitTypeData.SightRange);
            foreach (var scout in scouts) {
                var otherDistances = 1 + distances.Where(kv => kv.Key != scout).Sum(kv => Math.Min(maxDensityDistance, kv.Value));
                density[scout] = 10000 / otherDistances;
            }

            distanceToScouts[cellToScout] = distances;
            densityScores[cellToScout] = density;
        }

        foreach (var scout in scouts) {
            var cellToExplore = _areaToScout.MinBy(cell => {
                var edgenessScore = _edgenessScore[cell];
                var densityScore = densityScores[cell][scout];

                var cellDistances = distanceToScouts[cell];
                var myDistance = cellDistances[scout];
                var minDistance = cellDistances.Min(kv => kv.Value);
                var maxDistance = cellDistances.Max(kv => kv.Value);

                var penalty = 1f;
                if (maxDistance > minDistance) {
                    penalty += (myDistance - minDistance) / (maxDistance - minDistance);
                }

                return (edgenessScore + densityScore) * penalty;
            });

            scout.Move(cellToExplore);
            Program.GraphicalDebugger.AddLink(scout.Position, cellToExplore, Colors.LightBlue, withText: false);
        }
    }

    private void DebugAreaToScout() {
        if (!DrawEnabled) {
            return;
        }

        var minEdge = _edgenessScore.Values.Min();
        var maxEdge = _edgenessScore.Values.Max();

        foreach (var cell in _areaToScout) {
            Program.GraphicalDebugger.AddGridSquare(cell, Colors.LightBlue);

            var edgeScore = _edgenessScore[cell];
            var textColor = Colors.Gradient(Colors.DarkGrey, Colors.DarkRed, LogScale(edgeScore, minEdge, maxEdge));
            Program.GraphicalDebugger.AddText($"{edgeScore,4:F2}", size: 12, worldPos: cell.ToPoint(xOffset: -0.3f), color: textColor);
        }
    }

    private static float LogScale(float number, float min, float max) {
        var logNum = (float)Math.Log2(number + 1);
        var logMin = (float)Math.Log2(min + 1);
        var logMax = (float)Math.Log2(max + 1);

        return (logNum - logMin) / (logMax - logMin);
    }
}
