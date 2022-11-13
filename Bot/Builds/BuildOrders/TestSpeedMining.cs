using System.Collections.Generic;
using Bot.GameData;
using Bot.GameSense.EnemyStrategyTracking;

namespace Bot.Builds.BuildOrders;

public class TestSpeedMining : IBuildOrder {
    public IReadOnlyCollection<BuildRequest> BuildRequests { get; }

    public TestSpeedMining() {
        BuildRequests = new List<BuildRequest>
        {
            new QuantityBuildRequest(BuildType.Train, Units.Drone,    atSupply: 12),
            new QuantityBuildRequest(BuildType.Train, Units.Overlord, atSupply: 13),
            new TargetBuildRequest  (BuildType.Train, Units.Drone,    atSupply: 13, targetQuantity: 16),
        };
    }

    public void PruneRequests() {}

    public void AddRequest(BuildRequest buildRequest) {}

    public void ReactTo(EnemyStrategy newEnemyStrategy) {}
}
