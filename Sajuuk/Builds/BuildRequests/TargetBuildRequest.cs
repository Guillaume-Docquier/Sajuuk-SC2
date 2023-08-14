using System;
using System.Linq;
using Sajuuk.GameData;
using Sajuuk.GameSense;

namespace Sajuuk.Builds.BuildRequests;

public class TargetBuildRequest : BuildRequest {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IController _controller;

    private int _quantityTrulyFulfilled = 0;

    public override int QuantityFulfilled {
        get {
            if (BuildType == BuildType.Research) {
                return _controller.ResearchedUpgrades.Contains(UnitOrUpgradeType) || _controller.IsResearchInProgress(UnitOrUpgradeType) ? 1 : 0;
            }

            var existingUnitsOrBuildings = _unitsTracker.GetUnits(_unitsTracker.OwnedUnits, UnitOrUpgradeType);
            if (Units.Extractors.Contains(UnitOrUpgradeType)) {
                // TODO GD When losing the natural, this causes the build to be stuck because it will try to replace gasses that are already taken
                // Ignore extractors that are not assigned to a townhall. This way we can target X working extractors
                existingUnitsOrBuildings = existingUnitsOrBuildings.Where(extractor => extractor.Supervisor != null);
            }

            return existingUnitsOrBuildings.Count() + _controller.GetProducersCarryingOrders(UnitOrUpgradeType).Count();
        }
    }
    public override int QuantityRemaining => Math.Max(0, QuantityRequested - QuantityFulfilled);

    public TargetBuildRequest(
        IUnitsTracker unitsTracker,
        IController controller,
        BuildType buildType,
        uint unitOrUpgradeType,
        int targetQuantity,
        uint atSupply,
        bool queue,
        BuildBlockCondition blockCondition,
        BuildRequestPriority priority
    )
        : base(buildType, unitOrUpgradeType, targetQuantity, atSupply, queue, blockCondition, priority) {
        _unitsTracker = unitsTracker;
        _controller = controller;
    }

    public override void Fulfill(int quantity) {
        // We don't actually use it, but it is useful to see how many units were requested because of this request
        _quantityTrulyFulfilled++;
    }
}