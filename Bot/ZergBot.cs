using System.Collections.Generic;
using SC2APIProtocol;

namespace Bot;

using BuildOrder = Queue<BuildOrders.BuildStep>;

public class MiningBot: PoliteBot {
    private static readonly BuildOrder BuildOrder = BuildOrders.TwoBasesRoach();

    public override string Name => "ZergBot";

    public override Race Race => Race.Zerg;

    protected override void DoOnFrame() {
        FollowBuildOrder();
        if (!IsBuildOrderBlocking()) {
            SpawnDrones();
        }

        FastMining();
    }

    private void FollowBuildOrder() {
        if (BuildOrder.Count == 0) {
            return;
        }

        while(BuildOrder.Count > 0) {
            var buildStep = BuildOrder.Peek();
            if (Controller.CurrentSupply < buildStep.AtSupply || !Controller.ExecuteBuildStep(buildStep)) {
                return;
            }

            buildStep.Quantity -= 1;
            if (BuildOrder.Peek().Quantity == 0) {
                BuildOrder.Dequeue();
            }
        }
    }

    private bool IsBuildOrderBlocking() {
        return BuildOrder.Count > 0 && Controller.CurrentSupply >= BuildOrder.Peek().AtSupply;
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
