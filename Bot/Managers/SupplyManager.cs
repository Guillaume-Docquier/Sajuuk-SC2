using System;
using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.GameData;
using Bot.GameSense;
using Bot.Managers.BuildManagement;

namespace Bot.Managers;

public class SupplyManager : Manager {
    private const int SupplyPerHatchery = 6;
    private const int SupplyPerOverlord = 8;

    private const int SupplyCushion = 2;
    private const int OverlordBatchSize = 4;

    private readonly BuildManager _buildManager;
    private readonly BuildRequest _overlordsBuildRequest = new TargetBuildRequest(BuildType.Train, Units.Overlord, 0);
    private readonly List<BuildRequest> _buildRequests = new List<BuildRequest>();

    public override IEnumerable<BuildFulfillment> BuildFulfillments => _buildRequests.Select(buildRequest => buildRequest.Fulfillment);
    protected override IAssigner Assigner { get; }
    protected override IDispatcher Dispatcher { get; }
    protected override IReleaser Releaser { get; }

    public SupplyManager(BuildManager buildManager) {
        Assigner = new DummyAssigner();
        Dispatcher = new DummyDispatcher();
        Releaser = new DummyReleaser();

        _buildManager = buildManager;
        _buildRequests.Add(_overlordsBuildRequest);
    }

    protected override void AssignmentPhase() {}

    protected override void DispatchPhase() {}

    protected override void ManagementPhase() {
        MaintainOverlords();

        if (ShouldTakeOverOverlordManagement()) {
            if (ShouldRequestMoreOverlords()) {
                _overlordsBuildRequest.Requested += OverlordBatchSize;
            }

            _overlordsBuildRequest.Priority = BuildRequestPriority.Important;
        }
        else {
            _overlordsBuildRequest.Priority = BuildRequestPriority.Normal;
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
            .All(buildFulfilment => buildFulfilment.UnitOrUpgradeType != Units.Overlord);
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
