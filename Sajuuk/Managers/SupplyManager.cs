using System;
using System.Collections.Generic;
using System.Linq;
using Sajuuk.Builds.BuildRequests;
using Sajuuk.GameData;
using Sajuuk.GameSense;

namespace Sajuuk.Managers;

public class SupplyManager : UnitlessManager {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IController _controller;

    private const int SupplyPerHatchery = 6;
    private const int SupplyPerOverlord = 8;

    private const int SupplyCushion = 2;
    private const int OverlordBatchSize = 4;

    private readonly BuildManager _buildManager;

    private readonly BuildRequest _overlordsBuildRequest;
    private readonly List<BuildRequest> _buildRequests = new List<BuildRequest>();

    public override IEnumerable<IFulfillableBuildRequest> BuildRequests => _buildRequests;

    public SupplyManager(
        IUnitsTracker unitsTracker,
        IBuildRequestFactory buildRequestFactory,
        IController controller,
        BuildManager buildManager
    ) {
        _unitsTracker = unitsTracker;
        _controller = controller;

        _buildManager = buildManager;

        _overlordsBuildRequest = buildRequestFactory.CreateTargetBuildRequest(BuildType.Train, Units.Overlord, 0, priority: BuildRequestPriority.High);
        _buildRequests.Add(_overlordsBuildRequest);
    }

    protected override void ManagementPhase() {
        MaintainOverlords();

        if (ShouldTakeOverOverlordManagement() && ShouldRequestMoreOverlords()) {
            // TODO GD Be smarter about the batch size
            _overlordsBuildRequest.QuantityRequested += OverlordBatchSize;
        }
    }

    private void MaintainOverlords() {
        var supplyNeededFromOverlords = _controller.CurrentSupply - GetSupplyFromHatcheries();
        var requiredOverlords = (int)Math.Ceiling((float)supplyNeededFromOverlords / SupplyPerOverlord);

        _overlordsBuildRequest.QuantityRequested = requiredOverlords;
    }

    private bool ShouldTakeOverOverlordManagement() {
        return _buildManager.BuildRequests
            .Where(buildFulfilment => buildFulfilment.BuildType == BuildType.Train)
            .Where(buildFulfilment => buildFulfilment.UnitOrUpgradeType == Units.Overlord)
            .Sum(buildFulfilment => buildFulfilment.QuantityRemaining) <= 0;
    }

    private bool ShouldRequestMoreOverlords() {
        var requestedSupportedSupply = GetRequestedSupportedSupply();

        return requestedSupportedSupply < KnowledgeBase.MaxSupplyAllowed && requestedSupportedSupply <= _controller.CurrentSupply + SupplyCushion;
    }

    private int GetSupplyFromHatcheries() {
        return _unitsTracker.GetUnits(_unitsTracker.OwnedUnits, Units.Hatchery).Count(hatchery => hatchery.IsOperational) * SupplyPerHatchery;
    }

    private int GetRequestedSupportedSupply() {
        return _overlordsBuildRequest.QuantityRequested * SupplyPerOverlord + GetSupplyFromHatcheries();
    }
}
