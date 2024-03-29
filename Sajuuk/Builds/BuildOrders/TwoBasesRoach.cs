﻿using System.Collections.Generic;
using System.Linq;
using Sajuuk.Builds.BuildRequests;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.GameSense.EnemyStrategyTracking;

namespace Sajuuk.Builds.BuildOrders;

public class TwoBasesRoach : IBuildOrder {
    private readonly IUnitsTracker _unitsTracker;

    private EnemyStrategy _enemyStrategy = EnemyStrategy.Unknown;

    private List<BuildRequest> _buildRequests;
    public IReadOnlyCollection<BuildRequest> BuildRequests => _buildRequests;

    // TODO GD Tweak based on matchup?
    public TwoBasesRoach(
        IUnitsTracker unitsTracker,
        IBuildRequestFactory buildRequestFactory
    ) {
        _unitsTracker = unitsTracker;

        _buildRequests = new List<BuildRequest>
        {
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Train,       Units.Overlord,                    atSupply: 13, targetQuantity: 2),
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Expand,      Units.Hatchery,                    atSupply: 16),                    // TODO GD Need to be able to say 1 expand as opposed to 2 hatcheries
            // TODO GD The build requests are sorted by supply, so the pool actually gets built before the gas.
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Build,       Units.Extractor,                   atSupply: 18, targetQuantity: 1),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Build,       Units.SpawningPool,                atSupply: 17, targetQuantity: 1),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Train,       Units.Overlord,                    atSupply: 19, targetQuantity: 3),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Train,       Units.Queen,                       atSupply: 22, targetQuantity: 2),
            buildRequestFactory.CreateQuantityBuildRequest(BuildType.Train,       Units.Zergling,                    atSupply: 24, quantity: 2),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Train,       Units.Overlord,                    atSupply: 30, targetQuantity: 4),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Train,       Units.Queen,                       atSupply: 30, targetQuantity: 3),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.UpgradeInto, Units.Lair,                        atSupply: 33, targetQuantity: 1),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Build,       Units.RoachWarren,                 atSupply: 33, targetQuantity: 1),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Build,       Units.EvolutionChamber,            atSupply: 37, targetQuantity: 1),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Build,       Units.Extractor,                   atSupply: 37, targetQuantity: 2),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Research,    Upgrades.Burrow,                   atSupply: 40, targetQuantity: 1),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Train,       Units.Overlord,                    atSupply: 41, targetQuantity: 5),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Research,    Upgrades.ZergMissileWeaponsLevel1, atSupply: 44, targetQuantity: 1),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Build,       Units.Extractor,                   atSupply: 50, targetQuantity: 3),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Research,    Upgrades.TunnelingClaws,           atSupply: 50, targetQuantity: 1),
            buildRequestFactory.CreateTargetBuildRequest  (BuildType.Research,    Upgrades.GlialReconstitution,      atSupply: 50, targetQuantity: 1),
        };

        foreach (var buildRequest in _buildRequests) {
            buildRequest.Priority = BuildRequestPriority.BuildOrder;
            buildRequest.BlockCondition = BuildBlockCondition.MissingResources;
        }
    }

    public void PruneRequests() {
        _buildRequests = _buildRequests
            .Where(buildRequest => buildRequest is TargetBuildRequest || buildRequest.QuantityRemaining > 0)
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
        if (_unitsTracker.GetUnits(_unitsTracker.OwnedUnits, Units.RoachWarren).Any()) {
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
