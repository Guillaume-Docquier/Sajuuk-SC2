using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sajuuk.ExtensionMethods;
using Sajuuk.Builds.BuildRequests;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.GameSense.EnemyStrategyTracking;
using Sajuuk.UnitModules;
using SC2APIProtocol;

namespace Sajuuk.Debugging;

public class BotDebugger : IBotDebugger {
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly IDebuggingFlagsTracker _debuggingFlagsTracker;
    private readonly IUnitsTracker _unitsTracker;
    private readonly IIncomeTracker _incomeTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IEnemyStrategyTracker _enemyStrategyTracker;
    private readonly IEnemyRaceTracker _enemyRaceTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IController _controller;
    private readonly KnowledgeBase _knowledgeBase;
    private readonly ISpendingTracker _spendingTracker;

    public BotDebugger(
        IVisibilityTracker visibilityTracker,
        IDebuggingFlagsTracker debuggingFlagsTracker,
        IUnitsTracker unitsTracker,
        IIncomeTracker incomeTracker,
        ITerrainTracker terrainTracker,
        IEnemyStrategyTracker enemyStrategyTracker,
        IEnemyRaceTracker enemyRaceTracker,
        IGraphicalDebugger graphicalDebugger,
        IController controller,
        KnowledgeBase knowledgeBase,
        ISpendingTracker spendingTracker
    ) {
        _visibilityTracker = visibilityTracker;
        _debuggingFlagsTracker = debuggingFlagsTracker;
        _unitsTracker = unitsTracker;
        _incomeTracker = incomeTracker;
        _terrainTracker = terrainTracker;
        _enemyStrategyTracker = enemyStrategyTracker;
        _enemyRaceTracker = enemyRaceTracker;
        _graphicalDebugger = graphicalDebugger;
        _controller = controller;
        _knowledgeBase = knowledgeBase;
        _spendingTracker = spendingTracker;
    }

    public void Debug(List<IFulfillableBuildRequest> managerBuildRequests, (IFulfillableBuildRequest, BuildBlockCondition) buildBlockStatus) {
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
        DebugMatchupData();
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

        _graphicalDebugger.AddTextGroup(help, virtualPos: new Point { X = 0.02f, Y = 0.46f });
    }

    private void DebugBuildRequests(IReadOnlyCollection<IFulfillableBuildRequest> managerBuildRequests, (IFulfillableBuildRequest blockingStep, BuildBlockCondition blockingReason) buildBlockStatus) {
        if (!_debuggingFlagsTracker.IsActive(DebuggingFlags.BuildOrder)) {
            return;
        }

        var managersBuildStepsData = managerBuildRequests
            .Select(nextBuildRequest => {
                var buildRequestString = nextBuildRequest.ToString();
                if (buildBlockStatus.blockingStep == nextBuildRequest) {
                    buildRequestString += $" ({buildBlockStatus.blockingReason})";
                }

                return buildRequestString;
            })
            .Take(25)
            .ToList();

        managersBuildStepsData.Insert(0, $"Next {managersBuildStepsData.Count}/{managerBuildRequests.Count} build requests:\n");

        _graphicalDebugger.AddTextGroup(managersBuildStepsData, virtualPos: new Point { X = 0.02f, Y = 0.02f });
    }

    private void DebugEnemyDetectors() {
        if (!_debuggingFlagsTracker.IsActive(DebuggingFlags.EnemyDetectors)) {
            return;
        }

        var detectors = _unitsTracker.GetUnits(_unitsTracker.EnemyUnits, Units.Detectors);
        foreach (var detector in detectors) {
            _graphicalDebugger.AddText("!", size: 20, worldPos: detector.Position.AsWorldGridCenter().ToPoint(), color: Colors.Purple);
            _graphicalDebugger.AddGridSquaresInRadius(detector.Position.AsWorldGridCenter(), (int)detector.UnitTypeData.SightRange, Colors.Purple);
        }
    }

    private void DebugUnwalkableAreas() {
        if (!_debuggingFlagsTracker.IsActive(DebuggingFlags.UnwalkableAreas)) {
            return;
        }

        // We will ignore cells that are too low because we don't see them anyways
        // Showing all of them is too much for protobuf, the CodedOutputStream runs out of space
        var minHeightRequiredToShow = _terrainTracker.WalkableCells.Min(cell => _terrainTracker.WithWorldHeight(cell).Z) - 1;

        for (var x = 0; x < _terrainTracker.MaxX; x++) {
            for (var y = 0; y < _terrainTracker.MaxY; y++) {
                var position = _terrainTracker.WithWorldHeight(new Vector3(x, y, 0).AsWorldGridCenter());
                if (!_terrainTracker.IsWalkable(position) && position.Z >= minHeightRequiredToShow) {
                    _graphicalDebugger.AddGridSquare(position, Colors.LightRed);
                }
            }
        }
    }

    private void DebugIncomeRate() {
        if (!_debuggingFlagsTracker.IsActive(DebuggingFlags.IncomeRate)) {
            return;
        }

        _graphicalDebugger.AddTextGroup(new[]
        {
            "Resource income rates - past 30s",
        }, virtualPos: new Point { X = 0.315f, Y = 0.700f });

        var activeMiningModules = _unitsTracker.GetUnits(_unitsTracker.OwnedUnits, Units.Drone)
            .Select(UnitModule.Get<MiningModule>)
            .Where(module => module != null)
            .ToList();

        var mineralMiners = activeMiningModules.Count(module => module.ResourceType == Resources.ResourceType.Mineral);
        _graphicalDebugger.AddTextGroup(new[]
        {
            $"Minerals" + $"{$"({mineralMiners})",6}",
            $"Max: {_incomeTracker.MaxMineralsCollectionRate, 9:F0}",
            $"Average: {_incomeTracker.AverageMineralsCollectionRate, 5:F0}",
            $"Current: {_incomeTracker.CurrentMineralsCollectionRate, 5:F0}",
            $"Expected: {_incomeTracker.ExpectedMineralsCollectionRate, 4:F0}",
        }, virtualPos: new Point { X = 0.315f, Y = 0.725f });

        var vespeneMiners = activeMiningModules.Count(module => module.ResourceType == Resources.ResourceType.Gas);
        _graphicalDebugger.AddTextGroup(new[]
        {
            $"Vespene" + $"{$"({vespeneMiners})",7}",
            $"Max: {_incomeTracker.MaxVespeneCollectionRate, 9:F0}",
            $"Average: {_incomeTracker.AverageVespeneCollectionRate, 5:F0}",
            $"Current: {_incomeTracker.CurrentVespeneCollectionRate, 5:F0}",
            $"Expected: {_incomeTracker.ExpectedVespeneCollectionRate, 4:F0}",
        }, virtualPos: new Point { X = 0.410f, Y = 0.725f });
    }

    private void DebugFutureSpending() {
        if (!_debuggingFlagsTracker.IsActive(DebuggingFlags.Spend)) {
            return;
        }

        var mineralsToGasRatio = _spendingTracker.ExpectedFutureMineralsSpending / _spendingTracker.ExpectedFutureVespeneSpending;
        _graphicalDebugger.AddTextGroup(new[]
        {
            "Future spending",
            $"Minerals: {_spendingTracker.ExpectedFutureMineralsSpending, 8:F0}",
            $"Gas: {_spendingTracker.ExpectedFutureVespeneSpending, 13:F0}",
            // SC2 cannot render the infinity character, so we show "INF" instead
            $"Minerals/Gas: {(_spendingTracker.ExpectedFutureVespeneSpending == 0 ? "INF" : mineralsToGasRatio), 4:F1}",
        }, virtualPos: new Point { X = 0.505f, Y = 0.740f });
    }

    private void DebugEnemyGhostUnits() {
        if (!_debuggingFlagsTracker.IsActive(DebuggingFlags.GhostUnits)) {
            return;
        }

        foreach (var enemyGhostUnit in _unitsTracker.EnemyGhostUnits.Values) {
            _graphicalDebugger.AddUnit(enemyGhostUnit, Colors.Red);
        }
    }

    private void DebugKnownEnemyUnits() {
        if (!_debuggingFlagsTracker.IsActive(DebuggingFlags.KnownEnemyUnits)) {
            return;
        }

        var textGroup = new List<string>();

        textGroup.Add("Known enemy units\n");
        textGroup.AddRange(
            _unitsTracker.EnemyMemorizedUnits.Values
                .Concat(_unitsTracker.EnemyUnits.Where(enemy => !Units.Buildings.Contains(enemy.UnitType)))
                .GroupBy(unit => unit.UnitTypeData.Name)
                .OrderBy(group => group.Key)
                .Select(group => $"{group.Count()}x {group.Key}")
        );

        textGroup.Add("\nKnown enemy buildings\n");
        textGroup.AddRange(
            _unitsTracker.EnemyUnits.Where(enemy => Units.Buildings.Contains(enemy.UnitType))
                .GroupBy(unit => $"{unit.UnitTypeData.Name} {(unit.RawUnitData.DisplayType == DisplayType.Snapshot ? "(S)" : "")}")
                .OrderBy(group => group.Key)
                .Select(group => $"{group.Count()}x {group.Key}")
        );

        _graphicalDebugger.AddTextGroup(textGroup, virtualPos: new Point { X = 0.83f, Y = 0.20f });
    }

    private void DebugMatchupData() {
        if (!_debuggingFlagsTracker.IsActive(DebuggingFlags.MatchupData)) {
            return;
        }

        // TODO GD Add creep coverage
        var matchupTexts = new List<string>
        {
            $"Enemy: {_enemyRaceTracker.EnemyRace} / Visible: {_terrainTracker.VisibilityRatio,3:P0} / Explored: {_terrainTracker.ExplorationRatio,3:P0}",
            $"Strategy: {_enemyStrategyTracker.CurrentEnemyStrategy}"
        };

        _graphicalDebugger.AddTextGroup(matchupTexts, virtualPos: new Point { X = 0.50f, Y = 0.02f });
    }

    private void DebugExploration() {
        if (!_debuggingFlagsTracker.IsActive(DebuggingFlags.Exploration)) {
            return;
        }

        foreach (var notExploredCell in _terrainTracker.WalkableCells.Where(cell => !_visibilityTracker.IsExplored(cell))) {
            var color = Colors.PeachPink;
            _graphicalDebugger.AddText("?", color: color, worldPos: _terrainTracker.WithWorldHeight(notExploredCell).ToPoint());
            _graphicalDebugger.AddGridSquare(_terrainTracker.WithWorldHeight(notExploredCell), color: color);
        }
    }

    private void DebugUnitAndEffectNames() {
        if (!_debuggingFlagsTracker.IsActive(DebuggingFlags.Names)) {
            return;
        }

        foreach (var unit in _unitsTracker.UnitsByTag.Values) {
            var unitText = $"{unit.Name} ({unit.UnitType})";
            _graphicalDebugger.AddText(unitText, size: 11, worldPos: unit.Position.ToPoint(xOffset: -0.4f));
        }

        foreach (var effect in _controller.Observation.Observation.RawData.Effects) {
            foreach (var effectPosition in effect.Pos.Select(effectPosition => _terrainTracker.WithWorldHeight(new Vector3(effectPosition.X, effectPosition.Y, 0)))) {
                var effectText = $"{_knowledgeBase.GetEffectData(effect.EffectId).FriendlyName} ({effect.EffectId})";
                _graphicalDebugger.AddText(effectText, size: 11, worldPos: effectPosition.ToPoint(xOffset: -0.4f));
                _graphicalDebugger.AddSphere(effectPosition, effect.Radius, Colors.Cyan);
            }
        }
    }

    private void DebugCoordinates() {
        if (!_debuggingFlagsTracker.IsActive(DebuggingFlags.Coordinates)) {
            return;
        }

        foreach (var cell in _terrainTracker.WalkableCells) {
            var coords = new List<string>
            {
                $"{cell.X}",
                $"{cell.Y}"
            };
            _graphicalDebugger.AddTextGroup(coords, size: 10, worldPos: _terrainTracker.WithWorldHeight(cell).ToPoint(xOffset: -0.25f, yOffset: 0.2f));
        }
    }

    private void DebugRocks() {
        foreach (var neutralUnit in _unitsTracker.NeutralUnits) {
            var rockInfo = new []
            {
                neutralUnit.Name,
                neutralUnit.Position.ToString(),
            };

            _graphicalDebugger.AddTextGroup(rockInfo, worldPos: neutralUnit.Position.ToPoint());
        }
    }
}
