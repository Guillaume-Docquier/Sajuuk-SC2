using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapKnowledge;
using Bot.UnitModules;

namespace Bot.Managers;

public class CreepManager: UnitlessManager {
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly IUnitsTracker _unitsTracker;
    private readonly IMapAnalyzer _mapAnalyzer;
    private readonly IBuildingTracker _buildingTracker;

    public override IEnumerable<BuildFulfillment> BuildFulfillments => Enumerable.Empty<BuildFulfillment>();

    public CreepManager(
        IVisibilityTracker visibilityTracker,
        IUnitsTracker unitsTracker,
        IMapAnalyzer mapAnalyzer,
        IBuildingTracker buildingTracker
    ) {
        _visibilityTracker = visibilityTracker;
        _unitsTracker = unitsTracker;
        _mapAnalyzer = mapAnalyzer;
        _buildingTracker = buildingTracker;
    }

    protected override void ManagementPhase() {
        foreach (var creepTumor in Controller.GetUnits(_unitsTracker.NewOwnedUnits, Units.CreepTumor)) {
            TumorCreepSpreadModule.Install(creepTumor, _visibilityTracker, _mapAnalyzer, _buildingTracker);
        }
    }

    public override string ToString() {
        return "CreepManager";
    }
}
