using System;
using System.Linq;
using Bot.GameData;
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

    public BuildRequestPriority Priority => BuildRequest.Priority;

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

    public override int Fulfilled {
        get {
            if (BuildRequest.BuildType == BuildType.Research) {
                return Controller.ResearchedUpgrades.Contains(BuildRequest.UnitOrUpgradeType) || Controller.IsResearchInProgress(BuildRequest.UnitOrUpgradeType) ? 1 : 0;
            }

            var existingUnitsOrBuildings = Controller.GetUnits(UnitsTracker.OwnedUnits, BuildRequest.UnitOrUpgradeType);
            if (Units.Extractors.Contains(BuildRequest.UnitOrUpgradeType)) {
                // TODO GD When losing the natural, this causes the build to be stuck because it will try to replace gasses that are already taken
                // Ignore extractors that are not assigned to a townhall. This way we can target X working extractors
                existingUnitsOrBuildings = existingUnitsOrBuildings.Where(extractor => extractor.Supervisor != null);
            }

            return existingUnitsOrBuildings.Count() + Controller.GetProducersCarryingOrders(BuildRequest.UnitOrUpgradeType).Count();
        }
    }

    public override void Fulfill(int quantity) {
        // Do nothing
    }
}
