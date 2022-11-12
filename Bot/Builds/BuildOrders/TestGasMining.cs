using System.Collections.Generic;
using Bot.GameData;
using Bot.GameSense.EnemyStrategyTracking;

namespace Bot.Builds.BuildOrders;

public class TestGasMining : IBuildOrder {
    public List<BuildRequest> BuildRequests { get; }

    public TestGasMining() {
        BuildRequests = new List<BuildRequest>
        {
            new QuantityBuildRequest(BuildType.Build, Units.Extractor),
            new QuantityBuildRequest(BuildType.Train, Units.Overlord, atSupply: 13),
            new QuantityBuildRequest(BuildType.Build, Units.Extractor),
            new QuantityBuildRequest(BuildType.Train, Units.Overlord, atSupply: 19),
            new QuantityBuildRequest(BuildType.Train, Units.Overlord, atSupply: 19),
        };
    }

    public void ReactTo(EnemyStrategy enemyStrategy) {}
}
