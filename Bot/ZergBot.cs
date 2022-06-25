using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot;

using BuildOrder = Queue<BuildOrders.BuildStep>;

public class MiningBot: PoliteBot {
    private static readonly BuildOrder BuildOrder = BuildOrders.TwoBasesRoach();

    public override string Name => "ZergBot";

    public override Race Race => Race.Zerg;

    protected override async Task DoOnFrame() {
        await FollowBuildOrder();
        if (!IsBuildOrderBlocking()) {
            SpawnDrones();
        }

        FastMining();
    }

    private async Task FollowBuildOrder() {
        if (BuildOrder.Count == 0) {
            return;
        }

        while(BuildOrder.Count > 0) {
            var buildStep = BuildOrder.Peek();
            if (Controller.CurrentSupply < buildStep.AtSupply || !Controller.ExecuteBuildStep(buildStep)) {
                break;
            }

            buildStep.Quantity -= 1;
            if (BuildOrder.Peek().Quantity == 0) {
                BuildOrder.Dequeue();
            }
        }

        // Print debug info
        var nextBuildStepsData = BuildOrder
            .Take(3)
            .Select((nextBuildStep, index) => $"{nextBuildStep.BuildType.ToString()} {nextBuildStep.Quantity} {GameData.GetUnitTypeData(nextBuildStep.UnitOrAbilityType).Name} at {nextBuildStep.AtSupply} supply")
            .ToList();

        nextBuildStepsData.Insert(0, "Next 3 builds:");

        await Debugger.ShowDebugText(nextBuildStepsData);
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
