using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.GameData;
using Bot.GameSense;
using Bot.Utils;

namespace Bot.Managers;

public class UpgradesManager : UnitlessManager {
    private readonly BuildRequest _evolutionChamberBuildRequest = new TargetBuildRequest(BuildType.Build, Units.EvolutionChamber, targetQuantity: 0);
    private readonly List<BuildRequest> _buildRequests = new List<BuildRequest>();
    private readonly HashSet<uint> _requestedUpgrades = new HashSet<uint>();

    public override IEnumerable<BuildFulfillment> BuildFulfillments => _buildRequests.Select(request => request.Fulfillment);

    public UpgradesManager() {
        _buildRequests.Add(_evolutionChamberBuildRequest);
    }

    protected override void ManagementPhase() {
        // We assume the build order takes care of ZergMissileWeaponsLevel1
        if (!Controller.ResearchedUpgrades.Contains(Upgrades.ZergMissileWeaponsLevel1)) {
            return;
        }

        // We're done
        if (_requestedUpgrades.Contains(Upgrades.ZergMissileWeaponsLevel3) && _requestedUpgrades.Contains(Upgrades.ZergGroundArmorsLevel3)) {
            _evolutionChamberBuildRequest.Requested = 0;

            return;
        }

        var roachCount = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Roach).Count();

        // TODO GD We could probably use a state machine, these are always done sequentially
        AttemptResearchTier1(roachCount);
        ImproveResearchInfrastructure();
        AttemptResearchTier2(roachCount);
        AttemptResearchTier3(roachCount);
    }

    public override string ToString() {
        return "UpgradesManager";
    }

    private void AttemptResearchTier1(int roachCount) {
        if (_requestedUpgrades.Contains(Upgrades.ZergGroundArmorsLevel1)) {
            return;
        }

        if (roachCount >= 12) {
            _evolutionChamberBuildRequest.Requested = 1;
            _buildRequests.Add(new TargetBuildRequest(BuildType.Research, Upgrades.ZergGroundArmorsLevel1, targetQuantity: 1));
            _requestedUpgrades.Add(Upgrades.ZergGroundArmorsLevel1);
        }
    }

    private void ImproveResearchInfrastructure() {
        if (_evolutionChamberBuildRequest.Requested == 2) {
            return;
        }

        if (AreResearchedOrInProgress(new [] { Upgrades.ZergGroundArmorsLevel1 })) {
            var remainingResearchTime = GetRemainingResearchTime(Upgrades.ZergGroundArmorsLevel1);
            var evoChamberBuildTime = GetBuildTime(Units.EvolutionChamber);
            if (remainingResearchTime <= evoChamberBuildTime + 5) {
                _evolutionChamberBuildRequest.Requested = 2;
            }
        }
    }

    private void AttemptResearchTier2(int roachCount) {
        if (_requestedUpgrades.Contains(Upgrades.ZergMissileWeaponsLevel2)) {
            return;
        }

        if (!AreResearchedOrInProgress(new []{ Upgrades.ZergGroundArmorsLevel1, Upgrades.ZergMissileWeaponsLevel1 })) {
            return;
        }

        if (roachCount >= 27) {
            _buildRequests.Add(new TargetBuildRequest(BuildType.Build,    Units.Extractor,                   targetQuantity: 4));
            _buildRequests.Add(new TargetBuildRequest(BuildType.Research, Upgrades.ZergMissileWeaponsLevel2, targetQuantity: 1));
            _buildRequests.Add(new TargetBuildRequest(BuildType.Research, Upgrades.ZergGroundArmorsLevel2,   targetQuantity: 1));
            _requestedUpgrades.Add(Upgrades.ZergMissileWeaponsLevel2);
            _requestedUpgrades.Add(Upgrades.ZergGroundArmorsLevel2);
        }
    }

    private void AttemptResearchTier3(int roachCount) {
        if (_requestedUpgrades.Contains(Upgrades.ZergMissileWeaponsLevel3)) {
            return;
        }

        if (!AreResearchedOrInProgress(new []{ Upgrades.ZergGroundArmorsLevel2, Upgrades.ZergMissileWeaponsLevel2 })) {
            return;
        }

        if (roachCount >= 40) {
            _buildRequests.Add(new TargetBuildRequest  (BuildType.Build,       Units.InfestationPit,              targetQuantity: 1));
            _buildRequests.Add(new TargetBuildRequest  (BuildType.UpgradeInto, Units.Hive,                        targetQuantity: 1));
            _buildRequests.Add(new TargetBuildRequest  (BuildType.Research,    Upgrades.ZergMissileWeaponsLevel3, targetQuantity: 1));
            _buildRequests.Add(new TargetBuildRequest  (BuildType.Research,    Upgrades.ZergGroundArmorsLevel3,   targetQuantity: 1));
            _requestedUpgrades.Add(Upgrades.ZergMissileWeaponsLevel3);
            _requestedUpgrades.Add(Upgrades.ZergGroundArmorsLevel3);
        }
    }

    private static uint GetBuildTime(uint unitId) {
        var unitData = KnowledgeBase.GetUnitTypeData(unitId);

        return (uint)(unitData.BuildTime / TimeUtils.FramesPerSecond);
    }

    private static uint GetRemainingResearchTime(uint upgradeId) {
        if (Controller.ResearchedUpgrades.Contains(upgradeId)) {
            return 0;
        }

        var remainingPercent = 1 - Controller.GetResearchProgress(upgradeId);
        var upgradeData = KnowledgeBase.GetUpgradeData(upgradeId);

        return (uint)(upgradeData.ResearchTime / TimeUtils.FramesPerSecond * remainingPercent);
    }

    private static bool AreResearchedOrInProgress(IEnumerable<uint> upgradeIds) {
        return upgradeIds.All(upgradeId => Controller.ResearchedUpgrades.Contains(upgradeId) || Controller.IsResearchInProgress(upgradeId));
    }
}
