using System.Collections.Generic;
using System.Linq;
using Sajuuk.Builds;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.Utils;

namespace Sajuuk.Managers;

public class UpgradesManager : UnitlessManager {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IBuildRequestFactory _buildRequestFactory;
    private readonly IController _controller;
    private readonly KnowledgeBase _knowledgeBase;

    private readonly HashSet<uint> _requestedUpgrades = new HashSet<uint>();

    private readonly BuildRequest _evolutionChamberBuildRequest;
    private readonly List<BuildRequest> _buildRequests = new List<BuildRequest>();
    public override IEnumerable<IFulfillableBuildRequest> BuildRequests => _buildRequests;

    public UpgradesManager(
        IUnitsTracker unitsTracker,
        IBuildRequestFactory buildRequestFactory,
        IController controller,
        KnowledgeBase knowledgeBase
    ) {
        _unitsTracker = unitsTracker;
        _buildRequestFactory = buildRequestFactory;
        _controller = controller;
        _knowledgeBase = knowledgeBase;

        _evolutionChamberBuildRequest = _buildRequestFactory.CreateTargetBuildRequest(BuildType.Build, Units.EvolutionChamber, targetQuantity: 0);
        _buildRequests.Add(_evolutionChamberBuildRequest);
    }

    protected override void ManagementPhase() {
        // We assume the build order takes care of ZergMissileWeaponsLevel1
        if (!_controller.ResearchedUpgrades.Contains(Upgrades.ZergMissileWeaponsLevel1)) {
            return;
        }

        // We're done
        if (_requestedUpgrades.Contains(Upgrades.ZergMissileWeaponsLevel3) && _requestedUpgrades.Contains(Upgrades.ZergGroundArmorsLevel3)) {
            _evolutionChamberBuildRequest.QuantityRequested = 0;

            return;
        }

        var roachCount = _unitsTracker.GetUnits(_unitsTracker.OwnedUnits, Units.Roach).Count();

        // TODO GD We could probably use a state machine, these are always done sequentially
        AttemptResearchTier1(roachCount);
        ImproveResearchInfrastructure();
        AttemptResearchTier2(roachCount);
        AttemptResearchTier3(roachCount);

        foreach (var buildRequest in _buildRequests) {
            buildRequest.Priority = BuildRequestPriority.Medium;

            if (buildRequest.BuildType == BuildType.Research) {
                buildRequest.BlockCondition = BuildBlockCondition.MissingResources;
            }
        }
    }

    public override string ToString() {
        return "UpgradesManager";
    }

    private void AttemptResearchTier1(int roachCount) {
        if (_requestedUpgrades.Contains(Upgrades.ZergGroundArmorsLevel1)) {
            return;
        }

        if (roachCount >= 12) {
            _evolutionChamberBuildRequest.QuantityRequested = 1;
            _buildRequests.Add(_buildRequestFactory.CreateTargetBuildRequest(BuildType.Research, Upgrades.ZergGroundArmorsLevel1, targetQuantity: 1));
            _requestedUpgrades.Add(Upgrades.ZergGroundArmorsLevel1);
        }
    }

    private void ImproveResearchInfrastructure() {
        if (_evolutionChamberBuildRequest.QuantityRequested == 2) {
            return;
        }

        if (AreResearchedOrInProgress(new [] { Upgrades.ZergGroundArmorsLevel1 })) {
            var remainingResearchTime = GetRemainingResearchTime(Upgrades.ZergGroundArmorsLevel1);
            var evoChamberBuildTime = GetBuildTime(Units.EvolutionChamber);
            if (remainingResearchTime <= evoChamberBuildTime + 5) {
                _evolutionChamberBuildRequest.QuantityRequested = 2;
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
            _buildRequests.Add(_buildRequestFactory.CreateTargetBuildRequest(BuildType.Research, Upgrades.ZergMissileWeaponsLevel2, targetQuantity: 1));
            _buildRequests.Add(_buildRequestFactory.CreateTargetBuildRequest(BuildType.Research, Upgrades.ZergGroundArmorsLevel2,   targetQuantity: 1));
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
            _buildRequests.Add(_buildRequestFactory.CreateTargetBuildRequest(BuildType.Build,       Units.InfestationPit,              targetQuantity: 1));
            _buildRequests.Add(_buildRequestFactory.CreateTargetBuildRequest(BuildType.UpgradeInto, Units.Hive,                        targetQuantity: 1));
            _buildRequests.Add(_buildRequestFactory.CreateTargetBuildRequest(BuildType.Research,    Upgrades.ZergMissileWeaponsLevel3, targetQuantity: 1));
            _buildRequests.Add(_buildRequestFactory.CreateTargetBuildRequest(BuildType.Research,    Upgrades.ZergGroundArmorsLevel3,   targetQuantity: 1));
            _requestedUpgrades.Add(Upgrades.ZergMissileWeaponsLevel3);
            _requestedUpgrades.Add(Upgrades.ZergGroundArmorsLevel3);
        }
    }

    private uint GetBuildTime(uint unitId) {
        var unitData = _knowledgeBase.GetUnitTypeData(unitId);

        return (uint)(unitData.BuildTime / TimeUtils.FramesPerSecond);
    }

    private uint GetRemainingResearchTime(uint upgradeId) {
        if (_controller.ResearchedUpgrades.Contains(upgradeId)) {
            return 0;
        }

        var remainingPercent = 1 - _controller.GetResearchProgress(upgradeId);
        var upgradeData = _knowledgeBase.GetUpgradeData(upgradeId);

        return (uint)(upgradeData.ResearchTime / TimeUtils.FramesPerSecond * remainingPercent);
    }

    private bool AreResearchedOrInProgress(IEnumerable<uint> upgradeIds) {
        return upgradeIds.All(upgradeId => _controller.ResearchedUpgrades.Contains(upgradeId) || _controller.IsResearchInProgress(upgradeId));
    }
}
