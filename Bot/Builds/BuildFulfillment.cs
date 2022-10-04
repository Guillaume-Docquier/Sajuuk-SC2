using System;
using System.Linq;
using Bot.GameSense;

namespace Bot.Builds;

public abstract class BuildFulfillment {
    protected readonly BuildRequest BuildRequest;

    public BuildFulfillment(BuildRequest buildRequest) {
        BuildRequest = buildRequest;
    }

    public BuildType BuildType => BuildRequest.BuildType;

    public uint AtSupply => BuildRequest.AtSupply;

    public uint UnitOrUpgradeType => BuildRequest.UnitOrUpgradeType;

    public bool Queue => BuildRequest.Queue;

    public virtual int Remaining => Math.Max(0, BuildRequest.Requested - Fulfilled);
    public abstract int Fulfilled { get; }

    public abstract void Fulfill(int quantity);

    public override string ToString() {
        return BuildRequest.ToString();
    }
}

public class QuantityFulfillment: BuildFulfillment {
    public QuantityFulfillment(BuildRequest buildRequest) : base(buildRequest) {}

    private int _fulfilled = 0;
    public override int Fulfilled => _fulfilled;

    public override void Fulfill(int quantity) {
        _fulfilled += quantity;
    }
}

public class TargetFulfillment: BuildFulfillment {
    public TargetFulfillment(BuildRequest buildRequest) : base(buildRequest) {}

    // TODO GD Handle research?
    public override int Fulfilled => Controller.GetUnits(UnitsTracker.OwnedUnits, BuildRequest.UnitOrUpgradeType).Count()
                                     + Controller.GetProducersCarryingOrders(BuildRequest.UnitOrUpgradeType).Count();

    public override void Fulfill(int quantity) {
        // Do nothing
    }
}
