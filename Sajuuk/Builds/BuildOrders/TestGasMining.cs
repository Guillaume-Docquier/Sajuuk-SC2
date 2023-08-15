using System.Collections.Generic;
using Sajuuk.Builds.BuildRequests;
using Sajuuk.GameData;
using Sajuuk.GameSense.EnemyStrategyTracking;

namespace Sajuuk.Builds.BuildOrders;

public class TestGasMining : IBuildOrder {
    public IReadOnlyCollection<BuildRequest> BuildRequests { get; }

    public TestGasMining(IBuildRequestFactory buildRequestFactory) {
        BuildRequests = new List<BuildRequest>
        {
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train, Units.Drone,     atSupply: 12),
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train, Units.Overlord,  atSupply: 13),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Train, Units.Drone,     atSupply: 13, targetQuantity: 19),
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train, Units.Overlord,  atSupply: 19),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Train, Units.Drone,     atSupply: 19, targetQuantity: 27),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Build, Units.Extractor, atSupply: 24, targetQuantity: 2),
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train, Units.Overlord,  atSupply: 27),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Train, Units.Drone,     atSupply: 27, targetQuantity: 30),

            // Prevent anyone else from building anything else
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train, Units.Zergling, atSupply: 30),
        };

        foreach (var buildRequest in BuildRequests) {
            buildRequest.Priority = BuildRequestPriority.VeryHigh;
            buildRequest.BlockCondition = BuildBlockCondition.All;
        }
    }

    public void PruneRequests() {}

    public void ReactTo(EnemyStrategy newEnemyStrategy) {}
}
