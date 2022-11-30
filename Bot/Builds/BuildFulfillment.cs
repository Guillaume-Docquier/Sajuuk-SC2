using System;
using System.Linq;
using Bot.GameData;
using Bot.GameSense;

namespace Bot.Builds;

/// <summary>
/// A proxy class for a BuildRequest that prevents consumers from setting certain properties reserved to producers.
/// i.e: BuildRequest.Requested
///
/// It also provides a consumer focused API
/// i.e: Fulfill
/// </summary>
public abstract class BuildFulfillment {
    protected readonly BuildRequest BuildRequest;

    protected BuildFulfillment(BuildRequest buildRequest) {
        BuildRequest = buildRequest;
    }

    public BuildType BuildType => BuildRequest.BuildType;
    public uint UnitOrUpgradeType => BuildRequest.UnitOrUpgradeType;
    public uint AtSupply => BuildRequest.AtSupply;
    public bool Queue => BuildRequest.Queue;
    public BuildBlockCondition BlockCondition => BuildRequest.BlockCondition;

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

    private int _granted = 0;

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
        // We don't actually use it, but it is useful to see how many units were requested because of this request
        _granted++;
    }
}
