using System.Collections.Generic;
using System.Linq;
using Bot.GameData;
using Bot.GameSense;
using Bot.GameSense.EnemyStrategyTracking;

namespace Bot.Builds.BuildOrders;

/**
 * This build was created by optimizing TwoBasesRoach using SCFusion
 * https://github.com/andrew-j-armstrong/SCFusion
 *
 * It achieves slightly faster timings and you get 2 extra bases.
 * You need good scouting otherwise it's hard to hold the bases.
 */
public class FourBasesRoach : IBuildOrder {
    private readonly IUnitsTracker _unitsTracker;

    private EnemyStrategy _enemyStrategy = EnemyStrategy.Unknown;

    private List<BuildRequest> _buildRequests;
    public IReadOnlyCollection<BuildRequest> BuildRequests => _buildRequests;

    public FourBasesRoach(IUnitsTracker unitsTracker, IBuildRequestFactory buildRequestFactory) {
        _unitsTracker = unitsTracker;

        _buildRequests = new List<BuildRequest>
        {
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Train,       Units.Overlord,                    atSupply: 14, targetQuantity: 2),
            // TODO GD The build requests are sorted by supply, so the pool and extractor actually get built before the hatch.
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Expand,      Units.Hatchery,                    atSupply: 17),                    // TODO GD Need to be able to say 1 expand as opposed to 2 hatcheries
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Build,       Units.SpawningPool,                atSupply: 16, targetQuantity: 1),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Build,       Units.Extractor,                   atSupply: 17, targetQuantity: 1),
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Expand,      Units.Hatchery,                    atSupply: 19),
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train,       Units.Zergling,                    atSupply: 22, quantity: 1),
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Expand,      Units.Hatchery,                    atSupply: 24),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Build,       Units.EvolutionChamber,            atSupply: 28, targetQuantity: 1),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.UpgradeInto, Units.Lair,                        atSupply: 27, targetQuantity: 1),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Build,       Units.RoachWarren,                 atSupply: 31, targetQuantity: 1),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Research,    Upgrades.ZergMissileWeaponsLevel1, atSupply: 30, targetQuantity: 1),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Train,       Units.Queen,                       atSupply: 30, targetQuantity: 1),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Build,       Units.Extractor,                   atSupply: 33, targetQuantity: 2),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Train,       Units.Overlord,                    atSupply: 34, targetQuantity: 3),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Research,    Upgrades.TunnelingClaws,           atSupply: 41, targetQuantity: 1),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Train,       Units.Overlord,                    atSupply: 42, targetQuantity: 4),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Research,    Upgrades.Burrow,                   atSupply: 42, targetQuantity: 1),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Train,       Units.Overlord,                    atSupply: 50, targetQuantity: 5),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Train,       Units.Overlord,                    atSupply: 54, targetQuantity: 7),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Train,       Units.Queen,                       atSupply: 54, targetQuantity: 3),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Train,       Units.Roach,                       atSupply: 58, targetQuantity: 2),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Train,       Units.Overlord,                    atSupply: 62, targetQuantity: 7),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Train,       Units.Roach,                       atSupply: 62, targetQuantity: 9),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Research,    Upgrades.GlialReconstitution,      atSupply: 80, targetQuantity: 1),
        };

        foreach (var buildRequest in _buildRequests) {
            buildRequest.Priority = BuildRequestPriority.BuildOrder;
            buildRequest.BlockCondition = BuildBlockCondition.MissingResources;
        }
    }

    public void PruneRequests() {
        _buildRequests = _buildRequests
            .Where(buildRequest => buildRequest is TargetBuildRequest || buildRequest.Fulfillment.Remaining > 0)
            .ToList();
    }

    public void ReactTo(EnemyStrategy newEnemyStrategy) {
        if (_enemyStrategy == newEnemyStrategy) {
            return;
        }

        switch (newEnemyStrategy) {
            case EnemyStrategy.OneBase:
            case EnemyStrategy.AggressivePool:
                TransitionToDefensiveBuild();
                break;
            case EnemyStrategy.TwelvePool:
            case EnemyStrategy.ZerglingRush:
                TransitionToRushDefense();
                break;
        }

        _enemyStrategy = newEnemyStrategy;
    }

    private void TransitionToDefensiveBuild() {
        var evos = _buildRequests.Where(request => request.BuildType == BuildType.Build && request.UnitOrUpgradeType == Units.EvolutionChamber);
        var lair = _buildRequests.Where(request => request.BuildType == BuildType.UpgradeInto && request.UnitOrUpgradeType == Units.Lair);
        var upgrades = _buildRequests.Where(request => request.BuildType == BuildType.Research);
        var stepsToPushBack = evos.Concat(lair).Concat(upgrades).ToList();

        var atMaxSupply = _buildRequests.Max(request => request.AtSupply);
        foreach (var buildRequest in stepsToPushBack) {
            buildRequest.AtSupply = atMaxSupply;
        }

        _buildRequests = _buildRequests
            .Except(stepsToPushBack)
            .Concat(stepsToPushBack)
            .ToList();
    }

    private void TransitionToRushDefense() {
        if (Controller.GetUnits(_unitsTracker.OwnedUnits, Units.RoachWarren).Any()) {
            return;
        }

        var evos = _buildRequests.Where(request => request.BuildType == BuildType.Build && request.UnitOrUpgradeType == Units.EvolutionChamber);
        var lair = _buildRequests.Where(request => request.BuildType == BuildType.UpgradeInto && request.UnitOrUpgradeType == Units.Lair);
        var upgrades = _buildRequests.Where(request => request.BuildType == BuildType.Research);
        var stepsToPushBack = evos.Concat(lair).Concat(upgrades).ToList();

        var atMaxSupply = _buildRequests.Max(request => request.AtSupply);
        foreach (var buildRequest in stepsToPushBack) {
            buildRequest.AtSupply = atMaxSupply;
        }

        _buildRequests = _buildRequests
            .Except(stepsToPushBack)
            .Concat(stepsToPushBack)
            .ToList();
    }
}
