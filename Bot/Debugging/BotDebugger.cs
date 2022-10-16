﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Builds;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.Debugging;

public class BotDebugger {
    private float _maxMineralRate = 0;

    public void Debug(IEnumerable<BuildRequest> buildOrder, IEnumerable<BuildFulfillment> managerBuildRequests) {
        if (!Program.DebugEnabled) {
            return;
        }

        DebugHelp();
        DebugBuildOrder(buildOrder, managerBuildRequests);
        DebugEnemyDetectors();
        DebugWalkableAreas();
        DebugDestructibles();
        DebugIncomeRate();
        DebugEnemyGhostUnits();
        DebugKnownEnemyUnits();
        DebugMatchupData();
    }

    private static void DebugHelp() {
        if (!DebuggingFlagsTracker.ActiveDebuggingFlags.Contains(DebuggingFlags.Help)) {
            return;
        }

        var help = DebuggingFlagsTracker.AllDebuggingFlags
            .Select(flag => $"{flag,10} {(DebuggingFlagsTracker.ActiveDebuggingFlags.Contains(flag) ? "ON" : "OFF")}")
            .ToList();

        help.Insert(0, "Debug flags");

        Program.GraphicalDebugger.AddTextGroup(help, virtualPos: new Point { X = 0.02f, Y = 0.50f });
    }

    private static void DebugBuildOrder(IEnumerable<BuildRequest> buildOrder, IEnumerable<BuildFulfillment> managerBuildRequests) {
        if (!DebuggingFlagsTracker.ActiveDebuggingFlags.Contains(DebuggingFlags.BuildOrder)) {
            return;
        }

        var nextBuildStepsData = buildOrder
            .Take(3)
            .Select(nextBuildStep => nextBuildStep.ToString())
            .ToList();

        if (nextBuildStepsData.Count > 0) {
            nextBuildStepsData.Insert(0, $"Next {nextBuildStepsData.Count} bot builds:\n");
        }

        var managersBuildStepsData = managerBuildRequests
            .Select(nextBuildStep => nextBuildStep.ToString())
            .ToList();

        if (managersBuildStepsData.Count > 0) {
            nextBuildStepsData.Add($"\nNext {managersBuildStepsData.Count} manager requests:\n");
        }
        nextBuildStepsData.AddRange(managersBuildStepsData);

        Program.GraphicalDebugger.AddTextGroup(nextBuildStepsData, virtualPos: new Point { X = 0.02f, Y = 0.02f });
    }

    private static void DebugEnemyDetectors() {
        if (!DebuggingFlagsTracker.ActiveDebuggingFlags.Contains(DebuggingFlags.EnemyDetectors)) {
            return;
        }

        var detectors = Controller.GetUnits(UnitsTracker.EnemyUnits, Units.Detectors);
        foreach (var detector in detectors) {
            Program.GraphicalDebugger.AddText("!", size: 20, worldPos: detector.Position.ToPoint(), color: Colors.Purple);
            Program.GraphicalDebugger.AddGridSquaresInRadius(detector.Position, (int)detector.UnitTypeData.SightRange, Colors.Purple);
        }
    }

    private static void DebugWalkableAreas() {
        if (!DebuggingFlagsTracker.ActiveDebuggingFlags.Contains(DebuggingFlags.WalkableAreas)) {
            return;
        }

        for (var x = 0; x < MapAnalyzer.MaxX; x++) {
            for (var y = 0; y < MapAnalyzer.MaxY; y++) {
                var position = new Vector3(x, y, 0).AsWorldGridCenter().WithWorldHeight();
                if (!MapAnalyzer.IsWalkable(position)) {
                    Program.GraphicalDebugger.AddGridSquare(position, Colors.LightRed);
                }
            }
        }
    }

    private static void DebugDestructibles() {
        if (!DebuggingFlagsTracker.ActiveDebuggingFlags.Contains(DebuggingFlags.Destructibles)) {
            return;
        }

        foreach (var unit in Controller.GetUnits(UnitsTracker.NeutralUnits, Units.Destructibles).ToList()) {
            Program.GraphicalDebugger.AddText(unit.Name, worldPos: unit.Position.ToPoint());
        }
    }

    private void DebugIncomeRate() {
        if (!DebuggingFlagsTracker.ActiveDebuggingFlags.Contains(DebuggingFlags.IncomeRate)) {
            return;
        }

        var scoreDetails = Controller.Observation.Observation.Score.ScoreDetails;
        _maxMineralRate = Math.Max(_maxMineralRate, scoreDetails.CollectionRateMinerals);
        Program.GraphicalDebugger.AddTextGroup(new[]
        {
            $"Max minerals rate: {_maxMineralRate}",
            $"Minerals rate: {scoreDetails.CollectionRateMinerals}",
        }, virtualPos: new Point { X = 0.315f, Y = 0.765f });
    }

    private static void DebugEnemyGhostUnits() {
        if (!DebuggingFlagsTracker.ActiveDebuggingFlags.Contains(DebuggingFlags.GhostUnits)) {
            return;
        }

        foreach (var enemyGhostUnit in UnitsTracker.EnemyGhostUnits.Values) {
            Program.GraphicalDebugger.AddUnit(enemyGhostUnit, Colors.Red);
        }
    }

    private static void DebugKnownEnemyUnits() {
        if (!DebuggingFlagsTracker.ActiveDebuggingFlags.Contains(DebuggingFlags.KnownEnemyUnits)) {
            return;
        }

        var textGroup = new List<string>();

        textGroup.Add("Known enemy units\n");
        textGroup.AddRange(
            UnitsTracker.EnemyMemorizedUnits.Values
                .Concat(UnitsTracker.EnemyUnits.Where(enemy => !Units.Buildings.Contains(enemy.UnitType)))
                .GroupBy(unit => unit.UnitTypeData.Name)
                .OrderBy(group => group.Key)
                .Select(group => $"{group.Count()}x {group.Key}")
        );

        textGroup.Add("\nKnown enemy buildings\n");
        textGroup.AddRange(
            UnitsTracker.EnemyUnits.Where(enemy => Units.Buildings.Contains(enemy.UnitType))
                .GroupBy(unit => $"{unit.UnitTypeData.Name} {(unit.RawUnitData.DisplayType == DisplayType.Snapshot ? "(S)" : "")}")
                .OrderBy(group => group.Key)
                .Select(group => $"{group.Count()}x {group.Key}")
        );

        Program.GraphicalDebugger.AddTextGroup(textGroup, virtualPos: new Point { X = 0.83f, Y = 0.20f });
    }

    private static void DebugMatchupData() {
        if (!DebuggingFlagsTracker.ActiveDebuggingFlags.Contains(DebuggingFlags.MatchupData)) {
            return;
        }

        // TODO GD Doesn't go over 94% visible/explored, not sure why
        var matchupText = $"Enemy: {Controller.EnemyRace} / Visible: {MapAnalyzer.VisibilityRatio,3:P0} / Explored: {MapAnalyzer.ExplorationRatio,3:P0}";
        Program.GraphicalDebugger.AddText(matchupText, virtualPos: new Point { X = 0.50f, Y = 0.02f });
    }
}