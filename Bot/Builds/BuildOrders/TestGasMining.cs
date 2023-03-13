using System.Collections.Generic;
using Bot.GameData;
using Bot.GameSense.EnemyStrategyTracking;

namespace Bot.Builds.BuildOrders;

public class TestGasMining : IBuildOrder {
    public IReadOnlyCollection<BuildRequest> BuildRequests { get; }

    public TestGasMining() {
        BuildRequests = new List<BuildRequest>
        {
            new QuantityBuildRequest(BuildType.Train, Units.Drone,     atSupply: 12),
            new QuantityBuildRequest(BuildType.Train, Units.Overlord,  atSupply: 13),
            new TargetBuildRequest  (BuildType.Train, Units.Drone,     atSupply: 13, targetQuantity: 19),
            new QuantityBuildRequest(BuildType.Train, Units.Overlord,  atSupply: 19),
            new TargetBuildRequest  (BuildType.Train, Units.Drone,     atSupply: 19, targetQuantity: 27),
            new TargetBuildRequest  (BuildType.Build, Units.Extractor, atSupply: 24, targetQuantity: 2),
            new QuantityBuildRequest(BuildType.Train, Units.Overlord,  atSupply: 27),
            new TargetBuildRequest  (BuildType.Train, Units.Drone,     atSupply: 27, targetQuantity: 30),

            // Prevent anyone else from building anything else
            new QuantityBuildRequest(BuildType.Train, Units.Zergling, atSupply: 30),
        };

        foreach (var buildRequest in BuildRequests) {
            buildRequest.Priority = BuildRequestPriority.VeryHigh;
            buildRequest.BlockCondition = BuildBlockCondition.All;
        }
    }

    public void PruneRequests() {}

    public void ReactTo(EnemyStrategy newEnemyStrategy) {}
}
