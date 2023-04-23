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
using Bot.UnitModules;
using SC2APIProtocol;

namespace Bot.Debugging;

public class BotDebugger {
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly IDebuggingFlagsTracker _debuggingFlagsTracker;

    public BotDebugger(IVisibilityTracker visibilityTracker, IDebuggingFlagsTracker debuggingFlagsTracker) {
        _visibilityTracker = visibilityTracker;
        _debuggingFlagsTracker = debuggingFlagsTracker;
    }

    public void Debug(List<BuildFulfillment> managerBuildRequests, (BuildFulfillment, BuildBlockCondition) buildBlockStatus, Race enemyRace) {
        if (!Program.DebugEnabled) {
            return;
        }

        DebugHelp();
        DebugBuildRequests(managerBuildRequests, buildBlockStatus);
        DebugEnemyDetectors();
        DebugUnwalkableAreas();
        DebugIncomeRate();
        DebugFutureSpending();
        DebugEnemyGhostUnits();
        DebugKnownEnemyUnits();
        DebugMatchupData(enemyRace);
        DebugExploration();
        DebugUnitAndEffectNames();
        DebugCoordinates();
        //DebugRocks();
    }

    private void DebugHelp() {
        if (!_debuggingFlagsTracker.IsActive(DebuggingFlags.Help)) {
            return;
        }

        var help = DebuggingFlags.GetAll()
            .Select(flag => $"{flag,-12} {(_debuggingFlagsTracker.IsActive(flag) ? "ON" : "OFF")}")
            .Take(14)
            .ToList();

        help.Insert(0, "Debug flags");

        Program.GraphicalDebugger.AddTextGroup(help, virtualPos: new Point { X = 0.02f, Y = 0.46f });
    }

    private void DebugBuildRequests(IReadOnlyCollection<BuildFulfillment> managerBuildRequests, (BuildFulfillment blockingStep, BuildBlockCondition blockingReason) buildBlockStatus) {
        if (!_debuggingFlagsTracker.IsActive(DebuggingFlags.BuildOrder)) {
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

    private void DebugEnemyDetectors() {
        if (!_debuggingFlagsTracker.IsActive(DebuggingFlags.EnemyDetectors)) {
            return;
        }

        var detectors = Controller.GetUnits(UnitsTracker.EnemyUnits, Units.Detectors);
        foreach (var detector in detectors) {
            Program.GraphicalDebugger.AddText("!", size: 20, worldPos: detector.Position.AsWorldGridCenter().ToPoint(), color: Colors.Purple);
            Program.GraphicalDebugger.AddGridSquaresInRadius(detector.Position.AsWorldGridCenter(), (int)detector.UnitTypeData.SightRange, Colors.Purple);
        }
    }

    private void DebugUnwalkableAreas() {
        if (!_debuggingFlagsTracker.IsActive(DebuggingFlags.UnwalkableAreas)) {
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
        if (!_debuggingFlagsTracker.IsActive(DebuggingFlags.IncomeRate)) {
            return;
        }

        Program.GraphicalDebugger.AddTextGroup(new[]
        {
            "Resource income rates - past 30s",
        }, virtualPos: new Point { X = 0.315f, Y = 0.700f });

        var activeMiningModules = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Drone)
            .Select(UnitModule.Get<MiningModule>)
            .Where(module => module != null)
            .ToList();

        var mineralMiners = activeMiningModules.Count(module => module.ResourceType == Resources.ResourceType.Mineral);
        Program.GraphicalDebugger.AddTextGroup(new[]
        {
            $"Minerals" + $"{$"({mineralMiners})",6}",
            $"Max: {IncomeTracker.Instance.MaxMineralsCollectionRate, 9:F0}",
            $"Average: {IncomeTracker.Instance.AverageMineralsCollectionRate, 5:F0}",
            $"Current: {IncomeTracker.Instance.CurrentMineralsCollectionRate, 5:F0}",
            $"Expected: {IncomeTracker.Instance.ExpectedMineralsCollectionRate, 4:F0}",
        }, virtualPos: new Point { X = 0.315f, Y = 0.725f });

        var vespeneMiners = activeMiningModules.Count(module => module.ResourceType == Resources.ResourceType.Gas);
        Program.GraphicalDebugger.AddTextGroup(new[]
        {
            $"Vespene" + $"{$"({vespeneMiners})",7}",
            $"Max: {IncomeTracker.Instance.MaxVespeneCollectionRate, 9:F0}",
            $"Average: {IncomeTracker.Instance.AverageVespeneCollectionRate, 5:F0}",
            $"Current: {IncomeTracker.Instance.CurrentVespeneCollectionRate, 5:F0}",
            $"Expected: {IncomeTracker.Instance.ExpectedVespeneCollectionRate, 4:F0}",
        }, virtualPos: new Point { X = 0.410f, Y = 0.725f });
    }

    private void DebugFutureSpending() {
        if (!_debuggingFlagsTracker.IsActive(DebuggingFlags.Spend)) {
            return;
        }

        var mineralsToGasRatio = SpendingTracker.Instance.ExpectedFutureMineralsSpending / SpendingTracker.Instance.ExpectedFutureVespeneSpending;
        Program.GraphicalDebugger.AddTextGroup(new[]
        {
            "Future spending",
            $"Minerals: {SpendingTracker.Instance.ExpectedFutureMineralsSpending, 8:F0}",
            $"Gas: {SpendingTracker.Instance.ExpectedFutureVespeneSpending, 13:F0}",
            // SC2 cannot render the infinity character, so we show "INF" instead
            $"Minerals/Gas: {(SpendingTracker.Instance.ExpectedFutureVespeneSpending == 0 ? "INF" : mineralsToGasRatio), 4:F1}",
        }, virtualPos: new Point { X = 0.505f, Y = 0.740f });
    }

    private void DebugEnemyGhostUnits() {
        if (!_debuggingFlagsTracker.IsActive(DebuggingFlags.GhostUnits)) {
            return;
        }

        foreach (var enemyGhostUnit in UnitsTracker.EnemyGhostUnits.Values) {
            Program.GraphicalDebugger.AddUnit(enemyGhostUnit, Colors.Red);
        }
    }

    private void DebugKnownEnemyUnits() {
        if (!_debuggingFlagsTracker.IsActive(DebuggingFlags.KnownEnemyUnits)) {
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

    private void DebugMatchupData(Race enemyRace) {
        if (!_debuggingFlagsTracker.IsActive(DebuggingFlags.MatchupData)) {
            return;
        }

        // TODO GD Add creep coverage
        var matchupTexts = new List<string>
        {
            $"Enemy: {enemyRace} / Visible: {MapAnalyzer.Instance.VisibilityRatio,3:P0} / Explored: {MapAnalyzer.Instance.ExplorationRatio,3:P0}",
            $"Strategy: {EnemyStrategyTracker.Instance.CurrentEnemyStrategy}"
        };

        Program.GraphicalDebugger.AddTextGroup(matchupTexts, virtualPos: new Point { X = 0.50f, Y = 0.02f });
    }

    private void DebugExploration() {
        if (!_debuggingFlagsTracker.IsActive(DebuggingFlags.Exploration)) {
            return;
        }

        foreach (var notExploredCell in MapAnalyzer.WalkableCells.Where(cell => !_visibilityTracker.IsExplored(cell))) {
            var color = Colors.PeachPink;
            Program.GraphicalDebugger.AddText("?", color: color, worldPos: notExploredCell.ToVector3().ToPoint());
            Program.GraphicalDebugger.AddGridSquare(notExploredCell.ToVector3(), color: color);
        }
    }

    private void DebugUnitAndEffectNames() {
        if (!_debuggingFlagsTracker.IsActive(DebuggingFlags.Names)) {
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

    private void DebugCoordinates() {
        if (!_debuggingFlagsTracker.IsActive(DebuggingFlags.Coordinates)) {
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

    private void DebugRocks() {
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
