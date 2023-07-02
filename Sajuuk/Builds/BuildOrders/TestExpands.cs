using System.Collections.Generic;
using Sajuuk.GameData;
using Sajuuk.GameSense.EnemyStrategyTracking;

namespace Sajuuk.Builds.BuildOrders;

public class TestExpands : IBuildOrder {
    public IReadOnlyCollection<BuildRequest> BuildRequests { get; }

    public TestExpands(IBuildRequestFactory buildRequestFactory) {
        BuildRequests = new List<BuildRequest>
        {
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train,  Units.Overlord,     atSupply: 13),
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Build,  Units.Extractor,    atSupply: 16),
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Expand, Units.Hatchery,     atSupply: 16),
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Build,  Units.SpawningPool, atSupply: 17),
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train,  Units.Overlord,     atSupply: 19),
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Build,  Units.Extractor,    atSupply: 20, quantity: 3),
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train,  Units.Queen,        atSupply: 20),
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Expand, Units.Hatchery,     atSupply: 24),
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train,  Units.Overlord,     atSupply: 30, quantity: 2),
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train,  Units.Queen,        atSupply: 30, quantity: 2),
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train,  Units.Overlord,     atSupply: 50),
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train,  Units.Overlord,     atSupply: 63),
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train,  Units.Overlord,     atSupply: 70, quantity: 6),
        };
    }

    public void PruneRequests() {}

    public void ReactTo(EnemyStrategy newEnemyStrategy) {}
}
