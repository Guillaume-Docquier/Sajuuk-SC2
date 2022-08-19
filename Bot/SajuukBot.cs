using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Builds;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.Managers;
using Bot.MapKnowledge;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot;

using BuildOrder = LinkedList<BuildRequest>;

public class SajuukBot: PoliteBot {
    private readonly BuildOrder _buildOrder = BuildOrders.TwoBasesRoach();
    private readonly List<IManager> _managers = new List<IManager>();

    private const int PriorityChangePeriod = 100;
    private int _managerPriorityIndex = 0;

    private float _maxMineralRate = 0;

    public override string Name => "SajuukBot";

    public override Race Race => Race.Zerg;

    public SajuukBot(string version) : base(version) {}

    protected override void DoOnFrame() {
        if (Controller.Frame == 0) {
            InitManagers();
        }

        FollowBuildOrder();

        _managers.ForEach(manager => manager.OnFrame());

        // TODO GD Most likely we will consume all the larvae in one shot
        // TODO GD A larvae round robin might be more interesting?
        // Make sure every one gets a turn
        if (Controller.Frame % PriorityChangePeriod == 0) {
            _managerPriorityIndex = (_managerPriorityIndex + 1) % _managers.Count;
        }

        AddressManagerRequests();
        FixSupply();

        DebugBuildOrder();
        DebugEnemyDetectors();
        // DebugWalkableAreas();
        // DebugDestructibles();
        DebugIncomeRate();

        foreach (var unit in UnitsTracker.UnitsByTag.Values) {
            unit.ExecuteModules();
        }
    }

    private void InitManagers() {
        _managers.Add(new EconomyManager());
        _managers.Add(new WarManager());
        _managers.Add(new CreepManager());
    }

    private void DebugIncomeRate() {
        var scoreDetails = Controller.Observation.Observation.Score.ScoreDetails;
        _maxMineralRate = Math.Max(_maxMineralRate, scoreDetails.CollectionRateMinerals);
        GraphicalDebugger.AddTextGroup(new[]
        {
            $"Max minerals rate: {_maxMineralRate}",
            $"Minerals rate: {scoreDetails.CollectionRateMinerals}",
        }, virtualPos: new Point { X = 0.315f, Y = 0.765f });
    }

    private void FollowBuildOrder() {
        if (_buildOrder.Count == 0) {
            return;
        }

        while(_buildOrder.Count > 0) {
            var buildStep = _buildOrder.First();
            if (Controller.CurrentSupply < buildStep.AtSupply || Controller.ExecuteBuildStep(buildStep.Fulfillment) != Controller.RequestResult.Ok) {
                break;
            }

            buildStep.Fulfillment.Fulfill(1);
            if (_buildOrder.First().Fulfillment.Remaining == 0) {
                _buildOrder.RemoveFirst();
            }
        }
    }

    private void DebugBuildOrder() {
        var nextBuildStepsData = _buildOrder
            .Take(3)
            .Select(nextBuildStep => nextBuildStep.ToString())
            .ToList();

        if (nextBuildStepsData.Count > 0) {
            nextBuildStepsData.Insert(0, $"Next {nextBuildStepsData.Count} bot builds:\n");
        }

        var managersBuildStepsData = GetManagersBuildRequests()
            .Select(nextBuildStep => nextBuildStep.ToString())
            .ToList();

        if (managersBuildStepsData.Count > 0) {
            nextBuildStepsData.Add($"\nNext {managersBuildStepsData.Count} manager requests:\n");
        }
        nextBuildStepsData.AddRange(managersBuildStepsData);

        GraphicalDebugger.AddTextGroup(nextBuildStepsData, virtualPos: new Point { X = 0.02f, Y = 0.02f });
    }

    private static void DebugEnemyDetectors() {
        var detectors = Controller.GetUnits(UnitsTracker.EnemyUnits, Units.Detectors);
        foreach (var detector in detectors) {
            GraphicalDebugger.AddText("!", size: 20, worldPos: detector.Position.ToPoint(), color: Colors.Purple);
            GraphicalDebugger.AddGridSquaresInRadius(detector.Position, (int)detector.UnitTypeData.SightRange, Colors.Purple);
        }
    }

    private static void DebugWalkableAreas() {
        for (var x = 0; x < MapAnalyzer.MaxX; x++) {
            for (var y = 0; y < MapAnalyzer.MaxY; y++) {
                var position = new Vector3(x, y, 0).AsWorldGridCenter().WithWorldHeight();
                if (!MapAnalyzer.IsWalkable(position)) {
                    GraphicalDebugger.AddGridSquare(position, Colors.LightRed);
                }
            }
        }
    }

    private static void DebugDestructibles() {
        foreach (var unit in Controller.GetUnits(UnitsTracker.NeutralUnits, Units.Destructibles).ToList()) {
            GraphicalDebugger.AddText(unit.Name, worldPos: unit.Position.ToPoint());
        }
    }

    private void AddressManagerRequests() {
        // TODO GD Check if we should stop when we can't fulfill a build order
        // Do everything you can
        foreach (var buildStep in GetManagersBuildRequests()) {
            if (buildStep.AtSupply > Controller.CurrentSupply) {
                continue;
            }

            while (!IsBuildOrderBlocking() && buildStep.Remaining > 0) {
                var buildStepResult = Controller.ExecuteBuildStep(buildStep);
                if (buildStepResult == Controller.RequestResult.Ok) {
                    buildStep.Fulfill(1);
                    FollowBuildOrder(); // Sometimes the build order will be unblocked
                }
                // Don't retry expands if they are all taken
                else if (buildStep.BuildType == BuildType.Expand && buildStepResult == Controller.RequestResult.NoSuitableLocation) {
                    buildStep.Fulfill(1);
                }
                else {
                    break;
                }
            }

            // Ensure expands get made
            if (buildStep.BuildType == BuildType.Expand && buildStep.Remaining > 0) {
                break;
            }
        }
    }

    private bool IsBuildOrderBlocking() {
        // TODO GD Replace this by a 'Controller.ReserveMinerals' and 'Controller.ReserveGas' method
        return _buildOrder.Count > 0
               && _buildOrder.First().AtSupply <= Controller.CurrentSupply
               && Controller.IsUnlocked(_buildOrder.First().UnitOrUpgradeType);
    }

    private IEnumerable<BuildFulfillment> GetManagersBuildRequests() {
        var buildFulfillments = new List<BuildFulfillment>();
        for (var i = 0; i < _managers.Count; i++) {
            var managerIndex = (_managerPriorityIndex + i) % _managers.Count;

            buildFulfillments.AddRange(_managers[managerIndex].BuildFulfillments.Where(buildFulfillment => buildFulfillment.Remaining > 0));
        }

        // Prioritize expands
        return buildFulfillments.OrderBy(buildRequest => buildRequest.BuildType == BuildType.Expand ? 0 : 1);
    }

    // TODO GD Handle overlords dying early game
    private void FixSupply() {
        if (_buildOrder.Count <= 0
            && Controller.AvailableSupply <= 2
            && Controller.MaxSupply < KnowledgeBase.MaxSupplyAllowed
            && !Controller.GetProducersCarryingOrders(Units.Overlord).Any()) {
            _buildOrder.AddFirst(new QuantityBuildRequest(BuildType.Train, Units.Overlord, quantity: 4));
        }
    }
}
