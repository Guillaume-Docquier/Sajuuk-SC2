using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Builds;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.GameSense.EnemyStrategyTracking;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.Debugging;

public class BotDebugger {
    private float _maxMineralRate = 0;
    private float _maxVespeneRate = 0;

    public void Debug(List<BuildFulfillment> managerBuildRequests, (BuildFulfillment, BuildBlockCondition) buildBlockStatus) {
        if (!Program.DebugEnabled) {
            return;
        }

        DebugHelp();
        DebugBuildRequests(managerBuildRequests, buildBlockStatus);
        DebugEnemyDetectors();
        DebugUnwalkableAreas();
        DebugIncomeRate();
        DebugEnemyGhostUnits();
        DebugKnownEnemyUnits();
        DebugMatchupData();
        DebugExploration();
        DebugUnitAndEffectNames();
        DebugCoordinates();
        //DebugRocks();
    }

    private static void DebugHelp() {
        if (!DebuggingFlagsTracker.IsActive(DebuggingFlags.Help)) {
            return;
        }

        var help = DebuggingFlags.GetAll()
            .Select(flag => $"{flag,-12} {(DebuggingFlagsTracker.IsActive(flag) ? "ON" : "OFF")}")
            .Take(14)
            .ToList();

        help.Insert(0, "Debug flags");

        Program.GraphicalDebugger.AddTextGroup(help, virtualPos: new Point { X = 0.02f, Y = 0.46f });
    }

    private static void DebugBuildRequests(IReadOnlyCollection<BuildFulfillment> managerBuildRequests, (BuildFulfillment blockingStep, BuildBlockCondition blockingReason) buildBlockStatus) {
        if (!DebuggingFlagsTracker.IsActive(DebuggingFlags.BuildOrder)) {
            return;
        }

        var managersBuildStepsData = managerBuildRequests
            .Select(nextBuildStep => {
                var stepString = nextBuildStep.ToString();
                if (buildBlockStatus.blockingStep == nextBuildStep) {
                    stepString += $" ({buildBlockStatus.blockingReason})";
                }

                return stepString;
            })
            .Take(25)
            .ToList();

        managersBuildStepsData.Insert(0, $"Next {managersBuildStepsData.Count}/{managerBuildRequests.Count} build requests:\n");

        Program.GraphicalDebugger.AddTextGroup(managersBuildStepsData, virtualPos: new Point { X = 0.02f, Y = 0.02f });
    }

    private static void DebugEnemyDetectors() {
        if (!DebuggingFlagsTracker.IsActive(DebuggingFlags.EnemyDetectors)) {
            return;
        }

        var detectors = Controller.GetUnits(UnitsTracker.EnemyUnits, Units.Detectors);
        foreach (var detector in detectors) {
            Program.GraphicalDebugger.AddText("!", size: 20, worldPos: detector.Position.AsWorldGridCenter().ToPoint(), color: Colors.Purple);
            Program.GraphicalDebugger.AddGridSquaresInRadius(detector.Position.AsWorldGridCenter(), (int)detector.UnitTypeData.SightRange, Colors.Purple);
        }
    }

    private static void DebugUnwalkableAreas() {
        if (!DebuggingFlagsTracker.IsActive(DebuggingFlags.UnwalkableAreas)) {
            return;
        }

        // We will ignore cells that are too low because we don't see them anyways
        // Showing all of them is too much for protobuf, the CodedOutputStream runs out of space
        var minHeightRequiredToShow = MapAnalyzer.WalkableCells.Min(cell => cell.ToVector3().Z) - 1;

        for (var x = 0; x < MapAnalyzer.MaxX; x++) {
            for (var y = 0; y < MapAnalyzer.MaxY; y++) {
                var position = new Vector3(x, y, 0).AsWorldGridCenter().WithWorldHeight();
                if (!MapAnalyzer.IsWalkable(position) && position.Z >= minHeightRequiredToShow) {
                    Program.GraphicalDebugger.AddGridSquare(position, Colors.LightRed);
                }
            }
        }
    }

    private void DebugIncomeRate() {
        if (!DebuggingFlagsTracker.IsActive(DebuggingFlags.IncomeRate)) {
            return;
        }

        var scoreDetails = Controller.Observation.Observation.Score.ScoreDetails;

        _maxMineralRate = Math.Max(_maxMineralRate, scoreDetails.CollectionRateMinerals);
        Program.GraphicalDebugger.AddTextGroup(new[]
        {
            $"Max minerals rate: {_maxMineralRate, 4}",
            $"Minerals rate: {scoreDetails.CollectionRateMinerals, 8}",
        }, virtualPos: new Point { X = 0.315f, Y = 0.765f });

        _maxVespeneRate = Math.Max(_maxVespeneRate, scoreDetails.CollectionRateVespene);
        Program.GraphicalDebugger.AddTextGroup(new[]
        {
            $"Max vespene rate: {_maxVespeneRate, 4}",
            $"Vespene rate: {scoreDetails.CollectionRateVespene, 8}",
        }, virtualPos: new Point { X = 0.455f, Y = 0.765f });
    }

    private static void DebugEnemyGhostUnits() {
        if (!DebuggingFlagsTracker.IsActive(DebuggingFlags.GhostUnits)) {
            return;
        }

        foreach (var enemyGhostUnit in UnitsTracker.EnemyGhostUnits.Values) {
            Program.GraphicalDebugger.AddUnit(enemyGhostUnit, Colors.Red);
        }
    }

    private static void DebugKnownEnemyUnits() {
        if (!DebuggingFlagsTracker.IsActive(DebuggingFlags.KnownEnemyUnits)) {
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
        if (!DebuggingFlagsTracker.IsActive(DebuggingFlags.MatchupData)) {
            return;
        }

        // TODO GD Add creep coverage
        var matchupTexts = new List<string>
        {
            $"Enemy: {Controller.EnemyRace} / Visible: {MapAnalyzer.VisibilityRatio,3:P0} / Explored: {MapAnalyzer.ExplorationRatio,3:P0}",
            $"Strategy: {EnemyStrategyTracker.EnemyStrategy}"
        };

        Program.GraphicalDebugger.AddTextGroup(matchupTexts, virtualPos: new Point { X = 0.50f, Y = 0.02f });
    }

    private static void DebugExploration() {
        if (!DebuggingFlagsTracker.IsActive(DebuggingFlags.Exploration)) {
            return;
        }

        foreach (var notExploredCell in MapAnalyzer.WalkableCells.Where(cell => !VisibilityTracker.IsExplored(cell))) {
            var color = Colors.PeachPink;
            Program.GraphicalDebugger.AddText("?", color: color, worldPos: notExploredCell.ToVector3().ToPoint());
            Program.GraphicalDebugger.AddGridSquare(notExploredCell.ToVector3(), color: color);
        }
    }

    private static void DebugUnitAndEffectNames() {
        if (!DebuggingFlagsTracker.IsActive(DebuggingFlags.Names)) {
            return;
        }

        foreach (var unit in UnitsTracker.UnitsByTag.Values) {
            var unitText = $"{unit.Name} ({unit.UnitType})";
            Program.GraphicalDebugger.AddText(unitText, size: 11, worldPos: unit.Position.ToPoint(xOffset: -0.4f));
        }

        foreach (var effect in Controller.Observation.Observation.RawData.Effects) {
            foreach (var effectPosition in effect.Pos.Select(effectPosition => new Vector3(effectPosition.X, effectPosition.Y, 0).WithWorldHeight())) {
                var effectText = $"{KnowledgeBase.GetEffectData(effect.EffectId).FriendlyName} ({effect.EffectId})";
                Program.GraphicalDebugger.AddText(effectText, size: 11, worldPos: effectPosition.ToPoint(xOffset: -0.4f));
                Program.GraphicalDebugger.AddSphere(effectPosition, effect.Radius, Colors.Cyan);
            }
        }
    }

    private static void DebugCoordinates() {
        if (!DebuggingFlagsTracker.IsActive(DebuggingFlags.Coordinates)) {
            return;
        }

        foreach (var cell in MapAnalyzer.WalkableCells) {
            var coords = new List<string>
            {
                $"{cell.X}",
                $"{cell.Y}"
            };
            Program.GraphicalDebugger.AddTextGroup(coords, size: 10, worldPos: cell.ToVector3().ToPoint(xOffset: -0.25f, yOffset: 0.2f));
        }
    }

    private static void DebugRocks() {
        foreach (var neutralUnit in UnitsTracker.NeutralUnits) {
            var rockInfo = new []
            {
                neutralUnit.Name,
                neutralUnit.Position.ToString(),
            };

            Program.GraphicalDebugger.AddTextGroup(rockInfo, worldPos: neutralUnit.Position.ToPoint());
        }
    }
}
