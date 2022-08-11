using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.Managers;
using Bot.MapKnowledge;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot;

using BuildOrder = LinkedList<BuildOrders.BuildStep>;

public class SajuukBot: PoliteBot {
    private const int MaxDroneCount = 70;

    private readonly BuildOrder _buildOrder = BuildOrders.TwoBasesRoach();
    private readonly List<IManager> _managers = new List<IManager>();

    private const int PriorityChangePeriod = 100;
    private int _managerPriorityIndex = 0;

    public override string Name => "SajuukBot";

    public override Race Race => Race.Zerg;

    public SajuukBot(string version) : base(version) {}

    protected override void DoOnFrame() {
        if (Controller.Frame == 0) {
            _managers.Add(new EconomyManager());
            _managers.Add(new WarManager());
            _managers.Add(new CreepManager());
        }

        FollowBuildOrder();

        _managers.ForEach(manager => manager.OnFrame());

        // TODO GD Most likely we will consume all the larvae in one shot
        // TODO GD A larvae round robin might be more interesting?
        // Make sure every one gets a turn
        if (Controller.Frame % PriorityChangePeriod == 0) {
            _managerPriorityIndex = (_managerPriorityIndex + 1) % _managers.Count;
        }

        // TODO GD Check if we should stop when we can't fulfill a build order
        // Do everything you can
        foreach (var buildStep in GetManagersBuildRequests()) {
            // Cap the amount of drones
            if (buildStep.UnitOrUpgradeType == Units.Drone && HasEnoughDrones()) {
                continue;
            }

            while (!IsBuildOrderBlocking() && buildStep.Quantity > 0) {
                var buildStepResult = Controller.ExecuteBuildStep(buildStep);
                if (buildStepResult == Controller.RequestResult.Ok) {
                    buildStep.Quantity--;
                    FollowBuildOrder(); // Sometimes the build order will be unblocked
                }
                // Don't retry expands if they are all taken
                else if (buildStep.BuildType == BuildType.Expand && buildStepResult == Controller.RequestResult.NoSuitableLocation) {
                    buildStep.Quantity--;
                }
                else {
                    break;
                }
            }

            // Ensure expands get made
            if (buildStep.BuildType == BuildType.Expand && buildStep.Quantity > 0) {
                break;
            }
        }

        FixSupply();

        DebugBuildOrder();
        DebugEnemyDetectors();
        // DebugWalkableAreas();
        // DebugDestructibles();

        foreach (var unit in UnitsTracker.UnitsByTag.Values) {
            unit.ExecuteModules();
        }
    }

    private void FollowBuildOrder() {
        if (_buildOrder.Count == 0) {
            return;
        }

        while(_buildOrder.Count > 0) {
            var buildStep = _buildOrder.First();
            if (Controller.CurrentSupply < buildStep.AtSupply || Controller.ExecuteBuildStep(buildStep) != Controller.RequestResult.Ok) {
                break;
            }

            buildStep.Quantity -= 1;
            if (_buildOrder.First().Quantity == 0) {
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

    private bool IsBuildOrderBlocking() {
        // TODO GD Replace this by a 'Controller.ReserveMinerals' and 'Controller.ReserveGas' method
        // TODO GD Allow other stuff to happen if we have to wait for tech or something
        return _buildOrder.Count > 0 && Controller.CurrentSupply >= _buildOrder.First().AtSupply;
    }

    private IEnumerable<BuildOrders.BuildStep> GetManagersBuildRequests() {
        var buildRequests = new List<BuildOrders.BuildStep>();
        for (var i = 0; i < _managers.Count; i++) {
            var managerIndex = (_managerPriorityIndex + i) % _managers.Count;

            buildRequests.AddRange(_managers[managerIndex].BuildStepRequests.Where(buildRequest => buildRequest.Quantity > 0));
        }

        // Prioritize expands
        return buildRequests.OrderBy(buildRequest => buildRequest.BuildType == BuildType.Expand ? 0 : 1);
    }

    // TODO GD Handle overlords dying early game
    private void FixSupply() {
        if (_buildOrder.Count <= 0
            && Controller.AvailableSupply <= 2
            && Controller.MaxSupply < KnowledgeBase.MaxSupplyAllowed
            && !Controller.GetUnitsInProduction(Units.Overlord).Any()) {
            _buildOrder.AddFirst(new BuildOrders.BuildStep(BuildType.Train, 0, Units.Overlord, 4));
        }
    }

    private static bool HasEnoughDrones() {
        return Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Drone).Count() >= MaxDroneCount;
    }
}
