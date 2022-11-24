using System.Collections.Generic;
using Bot.GameData;
using Bot.GameSense.EnemyStrategyTracking;

namespace Bot.Builds.BuildOrders;

public class TestExpands : IBuildOrder {
    public IReadOnlyCollection<BuildRequest> BuildRequests { get; }

    public TestExpands() {
        BuildRequests = new List<BuildRequest>
        {
            new QuantityBuildRequest(BuildType.Train,  Units.Overlord,     atSupply: 13),
            new QuantityBuildRequest(BuildType.Build,  Units.Extractor,    atSupply: 16),
            new QuantityBuildRequest(BuildType.Expand, Units.Hatchery,     atSupply: 16),
            new QuantityBuildRequest(BuildType.Build,  Units.SpawningPool, atSupply: 17),
            new QuantityBuildRequest(BuildType.Train,  Units.Overlord,     atSupply: 19),
            new QuantityBuildRequest(BuildType.Build,  Units.Extractor,    atSupply: 20, quantity: 3),
            new QuantityBuildRequest(BuildType.Train,  Units.Queen,        atSupply: 20),
            new QuantityBuildRequest(BuildType.Expand, Units.Hatchery,     atSupply: 24),
            new QuantityBuildRequest(BuildType.Train,  Units.Overlord,     atSupply: 30, quantity: 2),
            new QuantityBuildRequest(BuildType.Train,  Units.Queen,        atSupply: 30, quantity: 2),
            new QuantityBuildRequest(BuildType.Train,  Units.Overlord,     atSupply: 50),
            new QuantityBuildRequest(BuildType.Train,  Units.Overlord,     atSupply: 63),
            new QuantityBuildRequest(BuildType.Train,  Units.Overlord,     atSupply: 70, quantity: 6),
        };
    }

    public void PruneRequests() {}

    public void ReactTo(EnemyStrategy newEnemyStrategy) {}
}
