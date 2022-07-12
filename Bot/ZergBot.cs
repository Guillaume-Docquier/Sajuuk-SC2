using System.Collections.Generic;
using System.Linq;
using Bot.Managers;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot;

using BuildOrder = Queue<BuildOrders.BuildStep>;

public class ZergBot: PoliteBot {
    private readonly BuildOrder _buildOrder = BuildOrders.TestExpands();
    private readonly List<IManager> _managers = new List<IManager>();

    public override string Name => "ZergBot";

    public override Race Race => Race.Zerg;

    protected override void DoOnFrame() {
        if (Controller.Frame == 0) {
            _managers.Add(new EconomyManager());
            _managers.Add(new WarManager());
        }

        // Some units died
        Controller.DeadOwnedUnits.ForEach(unit => unit.Died());

        FollowBuildOrder();
        if (!IsBuildOrderBlocking()) {
            SpawnDrones();
        }

        // Execute managers
        _managers.ForEach(manager => manager.OnFrame());
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

        DebugBuildOrder();
    }

    private void DebugBuildOrder() {
        Debugger.AddDebugText("Next 3 builds:");

        var nextBuildStepsData = _buildOrder
            .Take(3)
            .Select(nextBuildStep => {
                var buildStepUnitOrUpgradeName = nextBuildStep.BuildType == BuildType.Research
                    ? GameData.GetUpgradeData(nextBuildStep.UnitOrUpgradeType).Name
                    : $"{nextBuildStep.Quantity} {GameData.GetUnitTypeData(nextBuildStep.UnitOrUpgradeType).Name}";

                return $"{nextBuildStep.BuildType.ToString()} {buildStepUnitOrUpgradeName} at {nextBuildStep.AtSupply} supply";
            });

        Debugger.AddDebugText(nextBuildStepsData);
    }

    private bool IsBuildOrderBlocking() {
        // TODO GD Replace this by a 'Controller.ReserveMinerals' and 'Controller.ReserveGas' method
        // TODO GD Allow other stuff to happen if we have to wait for tech or something
        return _buildOrder.Count > 0 && Controller.CurrentSupply >= _buildOrder.Peek().AtSupply;
    }

    private void SpawnDrones() {
        while (Controller.TrainUnit(Units.Drone)) {}
    }
}
