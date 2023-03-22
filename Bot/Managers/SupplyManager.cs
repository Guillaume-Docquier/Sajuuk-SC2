using System;
using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.GameData;
using Bot.GameSense;

namespace Bot.Managers;

public class SupplyManager : UnitlessManager {
    private const int SupplyPerHatchery = 6;
    private const int SupplyPerOverlord = 8;

    private const int SupplyCushion = 2;
    private const int OverlordBatchSize = 4;

    private readonly BuildManager _buildManager;
    private readonly BuildRequest _overlordsBuildRequest = new TargetBuildRequest(BuildType.Train, Units.Overlord, 0, priority: BuildRequestPriority.High);
    private readonly List<BuildRequest> _buildRequests = new List<BuildRequest>();

    public override IEnumerable<BuildFulfillment> BuildFulfillments => _buildRequests.Select(buildRequest => buildRequest.Fulfillment);

    public SupplyManager(BuildManager buildManager) {
        _buildManager = buildManager;
        _buildRequests.Add(_overlordsBuildRequest);
    }

    protected override void ManagementPhase() {
        MaintainOverlords();

        if (ShouldTakeOverOverlordManagement() && ShouldRequestMoreOverlords()) {
            // TODO GD Be smarter about the batch size
            _overlordsBuildRequest.Requested += OverlordBatchSize;
        }

        if (IsSupplyCapped()) {
            _overlordsBuildRequest.Requested += 1;
        }
    }

    private void MaintainOverlords() {
        var supplyNeededFromOverlords = Controller.CurrentSupply - GetSupplyFromHatcheries();
        var requiredOverlords = (int)Math.Ceiling((float)supplyNeededFromOverlords / SupplyPerOverlord);

        _overlordsBuildRequest.Requested = requiredOverlords;
    }

    private bool ShouldTakeOverOverlordManagement() {
        return _buildManager.BuildFulfillments
            .Where(buildFulfilment => buildFulfilment.BuildType == BuildType.Train)
            .Where(buildFulfilment => buildFulfilment.UnitOrUpgradeType == Units.Overlord)
            .Sum(buildFulfilment => buildFulfilment.Remaining) <= 0;
    }

    private bool ShouldRequestMoreOverlords() {
        var requestedSupportedSupply = GetRequestedSupportedSupply();

        return requestedSupportedSupply < KnowledgeBase.MaxSupplyAllowed && requestedSupportedSupply <= Controller.CurrentSupply + SupplyCushion;
    }

    private int GetSupplyFromHatcheries() {
        return Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Hatchery).Count(hatchery => hatchery.IsOperational) * SupplyPerHatchery;
    }

    private int GetRequestedSupportedSupply() {
        return _overlordsBuildRequest.Requested * SupplyPerOverlord + GetSupplyFromHatcheries();
    }

    private bool IsSupplyCapped() {
        return _overlordsBuildRequest.Fulfillment.Remaining == 0 && Controller.IsSupplyCapped;
    }
}
