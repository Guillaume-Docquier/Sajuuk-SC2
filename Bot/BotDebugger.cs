using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Builds;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapKnowledge;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot;

public class BotDebugger {
    private float _maxMineralRate = 0;

    public void Debug(IEnumerable<BuildRequest> buildOrder, IEnumerable<BuildFulfillment> managerBuildRequests) {
        if (!Program.DebugEnabled) {
            return;
        }

        DebugBuildOrder(buildOrder, managerBuildRequests);
        DebugEnemyDetectors();
        // DebugWalkableAreas();
        // DebugDestructibles();
        DebugIncomeRate();
        DebugEnemyGhostUnits();
        DebugKnownEnemyUnits();
        DebugEnemyRace();
    }

    private static void DebugBuildOrder(IEnumerable<BuildRequest> buildOrder, IEnumerable<BuildFulfillment> managerBuildRequests) {
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
        var detectors = Controller.GetUnits(UnitsTracker.EnemyUnits, Units.Detectors);
        foreach (var detector in detectors) {
            Program.GraphicalDebugger.AddText("!", size: 20, worldPos: detector.Position.ToPoint(), color: Colors.Purple);
            Program.GraphicalDebugger.AddGridSquaresInRadius(detector.Position, (int)detector.UnitTypeData.SightRange, Colors.Purple);
        }
    }

    private static void DebugWalkableAreas() {
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
        foreach (var unit in Controller.GetUnits(UnitsTracker.NeutralUnits, Units.Destructibles).ToList()) {
            Program.GraphicalDebugger.AddText(unit.Name, worldPos: unit.Position.ToPoint());
        }
    }

    private void DebugIncomeRate() {
        var scoreDetails = Controller.Observation.Observation.Score.ScoreDetails;
        _maxMineralRate = Math.Max(_maxMineralRate, scoreDetails.CollectionRateMinerals);
        Program.GraphicalDebugger.AddTextGroup(new[]
        {
            $"Max minerals rate: {_maxMineralRate}",
            $"Minerals rate: {scoreDetails.CollectionRateMinerals}",
        }, virtualPos: new Point { X = 0.315f, Y = 0.765f });
    }

    private static void DebugEnemyGhostUnits() {
        foreach (var enemyGhostUnit in UnitsTracker.EnemyGhostUnits.Values) {
            Program.GraphicalDebugger.AddUnit(enemyGhostUnit, Colors.Red);
        }
    }

    private static void DebugKnownEnemyUnits() {
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

    private static void DebugEnemyRace() {
        Program.GraphicalDebugger.AddText($"Enemy race: {Controller.EnemyRace}", virtualPos: new Point { X = 0.83f, Y = 0.18f });
    }
}
