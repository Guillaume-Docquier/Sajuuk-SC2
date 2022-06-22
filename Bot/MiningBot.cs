using System.Collections.Generic;
using SC2APIProtocol;

namespace Bot;

using BuildOrder = Queue<BuildOrders.BuildStep>;

public class MiningBot: PoliteBot {
    private static readonly BuildOrder _buildOrder = BuildOrders.TwoBasesRoach();

    public override string Name => "MiningBot";

    public override Race Race => Race.Zerg;

    protected override void DoOnFrame() {
        FollowBuildOrder();
        if (!IsBuildOrderBlocking()) {
            SpawnDrones();
        }

        FastMining();
    }

    private void FollowBuildOrder() {
        if (_buildOrder.Count == 0) {
            return;
        }

        while(_buildOrder.Count > 0) {
            var buildStep = _buildOrder.Peek();
            if (Controller.CurrentSupply < buildStep.AtSupply || !Controller.ExecuteBuildStep(buildStep)) {
                return;
            }

            buildStep.Quantity -= 1;
            if (_buildOrder.Peek().Quantity == 0) {
                _buildOrder.Dequeue();
            }
        }
    }

    private bool IsBuildOrderBlocking() {
        return _buildOrder.Count > 0 && Controller.CurrentSupply >= _buildOrder.Peek().AtSupply;
    }

    private void SpawnDrones() {
        var larvae = Controller.GetAvailableLarvae();
        if (larvae.Count == 0 || Controller.AvailableSupply == 0) {
            return;
        }

        var canStillTrain = true;
        for (var i = 0; i < larvae.Count && canStillTrain; i++) {
            canStillTrain = Controller.TrainUnit(Units.Drone, larvae[i]);
        }
    }

    private void FastMining() {

    }
}
