using System.Collections.Generic;
using Bot.GameData;
using Bot.GameSense.EnemyStrategyTracking;

namespace Bot.Builds.BuildOrders;

public class TestMineralSaturatedMining : IBuildOrder {
    public IReadOnlyCollection<BuildRequest> BuildRequests { get; }

    public TestMineralSaturatedMining(IBuildRequestFactory buildRequestFactory) {
        BuildRequests = new List<BuildRequest>
        {
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train, Units.Drone,    atSupply: 12),
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train, Units.Overlord, atSupply: 13),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Train, Units.Drone,    atSupply: 13, targetQuantity: 19),
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train, Units.Overlord, atSupply: 19),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Train, Units.Drone,    atSupply: 19, targetQuantity: 24),

            // Prevent anyone else from building anything else
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train, Units.Zergling, atSupply: 24),
        };

        foreach (var buildRequest in BuildRequests) {
            buildRequest.Priority = BuildRequestPriority.VeryHigh;
            buildRequest.BlockCondition = BuildBlockCondition.All;
        }
    }

    public void PruneRequests() {}

    public void ReactTo(EnemyStrategy newEnemyStrategy) {}
}
