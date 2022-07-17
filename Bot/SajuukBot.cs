using System.Collections.Generic;
using System.Linq;
using Bot.Managers;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot;

using BuildOrder = Queue<BuildOrders.BuildStep>;

public class SajuukBot: PoliteBot {
    private readonly BuildOrder _buildOrder = BuildOrders.TwoBasesRoach();
    private readonly List<IManager> _managers = new List<IManager>();

    private const int PriorityChangePeriod = 100;
    private int _managerPriorityIndex = 0;

    public override string Name => "SajuukBot";

    public override Race Race => Race.Zerg;

    protected override void DoOnFrame() {
        if (Controller.Frame == 0) {
            _managers.Add(new EconomyManager());
            _managers.Add(new WarManager());
        }

        FollowBuildOrder();

        _managers.ForEach(manager => manager.OnFrame());

        // TODO GD Most likely we will consume all the larvae in one shot
        // TODO GD A larvae round robin might be more interesting?
        // Make sure every one gets a turn
        if (Controller.Frame % PriorityChangePeriod == 0) {
            _managerPriorityIndex = (_managerPriorityIndex + 1) % _managers.Count;
        }

        // Do everything you can
        foreach (var buildStep in GetManagersBuildRequests()) {
            while (!IsBuildOrderBlocking() && buildStep.Quantity > 0 && Controller.ExecuteBuildStep(buildStep)) {
                buildStep.Quantity--;
                FollowBuildOrder(); // Sometimes the build order will be unblocked
            }
        }

        DebugBuildOrder();

        foreach (var unit in Controller.UnitsByTag.Values) {
            unit.ExecuteModules();
        }
    }

    private void FollowBuildOrder() {
        if (_buildOrder.Count == 0) {
            return;
        }

        while(_buildOrder.Count > 0) {
            var buildStep = _buildOrder.Peek();
            if (Controller.CurrentSupply < buildStep.AtSupply || !Controller.ExecuteBuildStep(buildStep)) {
                break;
            }

            buildStep.Quantity -= 1;
            if (_buildOrder.Peek().Quantity == 0) {
                _buildOrder.Dequeue();
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

    private bool IsBuildOrderBlocking() {
        // TODO GD Replace this by a 'Controller.ReserveMinerals' and 'Controller.ReserveGas' method
        // TODO GD Allow other stuff to happen if we have to wait for tech or something
        return _buildOrder.Count > 0 && Controller.CurrentSupply >= _buildOrder.Peek().AtSupply;
    }

    private IEnumerable<BuildOrders.BuildStep> GetManagersBuildRequests() {
        for (var managerIndex = 0; managerIndex < _managers.Count; managerIndex++) {
            var index = (_managerPriorityIndex + managerIndex) % _managers.Count;

            foreach (var buildRequest in _managers[index].BuildStepRequests) {
                if (buildRequest.Quantity > 0) {
                    yield return buildRequest;
                }
            }
        }
    }
}
