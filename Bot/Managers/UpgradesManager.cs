﻿using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.GameData;
using Bot.GameSense;

namespace Bot.Managers;

public class UpgradesManager : Manager {
    private readonly BuildRequest _evolutionChamberBuildRequest = new TargetBuildRequest(BuildType.Build, Units.EvolutionChamber, targetQuantity: 0);
    private readonly List<BuildRequest> _buildRequests = new List<BuildRequest>();
    private readonly HashSet<uint> _requestedUpgrades = new HashSet<uint>();

    public override IEnumerable<BuildFulfillment> BuildFulfillments => _buildRequests.Select(request => request.Fulfillment);

    protected override IAssigner Assigner { get; } = new DummyAssigner();
    protected override IDispatcher Dispatcher { get; } = new DummyDispatcher();
    protected override IReleaser Releaser { get; } = new DummyReleaser();

    public UpgradesManager() {
        _buildRequests.Add(_evolutionChamberBuildRequest);
    }

    protected override void AssignUnits() {}

    protected override void DispatchUnits() {}

    protected override void Manage() {
        if (!Controller.ResearchedUpgrades.Contains(Upgrades.ZergMissileWeaponsLevel1)) {
            return;
        }

        if (_requestedUpgrades.Contains(Upgrades.ZergMissileWeaponsLevel3) && _requestedUpgrades.Contains(Upgrades.ZergGroundArmorsLevel3)) {
            _evolutionChamberBuildRequest.Requested = 0;

            return;
        }

        var roachCount = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Roach).Count();

        AttemptResearchTier1(roachCount);
        AttemptResearchTier2(roachCount);
        AttemptResearchTier3(roachCount);
    }

    private void AttemptResearchTier1(int roachCount) {
        if (roachCount >= 12 && !_requestedUpgrades.Contains(Upgrades.ZergGroundArmorsLevel1)) {
            _evolutionChamberBuildRequest.Requested = 1;
            _buildRequests.Add(new TargetBuildRequest(BuildType.Research, Upgrades.ZergGroundArmorsLevel1, targetQuantity: 1));
            _requestedUpgrades.Add(Upgrades.ZergGroundArmorsLevel1);
        }

        if (_evolutionChamberBuildRequest.Requested <= 2 && _requestedUpgrades.Contains(Upgrades.ZergGroundArmorsLevel1) && Controller.IsResearchInProgress(Upgrades.ZergGroundArmorsLevel1)) {
            var remainingResearchTime = GetRemainingResearchTime(Upgrades.ZergGroundArmorsLevel1);
            var evoChamberBuildTime = GetBuildTime(Units.EvolutionChamber);
            if (remainingResearchTime <= evoChamberBuildTime + 5) {
                _evolutionChamberBuildRequest.Requested = 2;
            }
        }
    }

    private void AttemptResearchTier2(int roachCount) {
        if (roachCount >= 27 && !_requestedUpgrades.Contains(Upgrades.ZergMissileWeaponsLevel2)) {
            _buildRequests.Add(new TargetBuildRequest(BuildType.Build,    Units.Extractor,                   targetQuantity: 4));
            _buildRequests.Add(new TargetBuildRequest(BuildType.Research, Upgrades.ZergMissileWeaponsLevel2, targetQuantity: 1));
            _buildRequests.Add(new TargetBuildRequest(BuildType.Research, Upgrades.ZergGroundArmorsLevel2,   targetQuantity: 1));
            _requestedUpgrades.Add(Upgrades.ZergMissileWeaponsLevel2);
            _requestedUpgrades.Add(Upgrades.ZergGroundArmorsLevel2);
        }
    }

    private void AttemptResearchTier3(int roachCount) {
        if (roachCount >= 40 && !_requestedUpgrades.Contains(Upgrades.ZergMissileWeaponsLevel3)) {
            _buildRequests.Add(new TargetBuildRequest  (BuildType.Build,       Units.InfestationPit,              targetQuantity: 1));
            _buildRequests.Add(new QuantityBuildRequest(BuildType.UpgradeInto, Units.Hive));
            _buildRequests.Add(new TargetBuildRequest  (BuildType.Research,    Upgrades.ZergMissileWeaponsLevel3, targetQuantity: 1));
            _buildRequests.Add(new TargetBuildRequest  (BuildType.Research,    Upgrades.ZergGroundArmorsLevel3,   targetQuantity: 1));
            _requestedUpgrades.Add(Upgrades.ZergMissileWeaponsLevel3);
            _requestedUpgrades.Add(Upgrades.ZergGroundArmorsLevel3);
        }
    }

    private static uint GetBuildTime(uint unitId) {
        var unitData = KnowledgeBase.GetUnitTypeData(unitId);

        return (uint)(unitData.BuildTime / Controller.FramesPerSecond);
    }

    private static uint GetRemainingResearchTime(uint upgradeId) {
        var remainingPercent = 1 - Controller.GetResearchProgress(upgradeId);
        var upgradeData = KnowledgeBase.GetUpgradeData(upgradeId);

        return (uint)(upgradeData.ResearchTime / Controller.FramesPerSecond * remainingPercent);
    }

    private class DummyAssigner : IAssigner {
        public void Assign(Unit unit) {}
    }

    private class DummyDispatcher : IDispatcher {
        public void Dispatch(Unit unit) {}
    }

    private class DummyReleaser : IReleaser {
        public void Release(Unit unit) {}
    }
}