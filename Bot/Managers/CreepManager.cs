using System.Collections.Generic;
using System.Linq;
using Bot.Builds;
using Bot.Debugging.GraphicalDebugging;
using Bot.GameData;
using Bot.GameSense;
using Bot.UnitModules;

namespace Bot.Managers;

public class CreepManager: UnitlessManager {
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IBuildingTracker _buildingTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly ICreepTracker _creepTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IFrameClock _frameClock;

    public override IEnumerable<BuildFulfillment> BuildFulfillments => Enumerable.Empty<BuildFulfillment>();

    public CreepManager(
        IVisibilityTracker visibilityTracker,
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IBuildingTracker buildingTracker,
        IRegionsTracker regionsTracker,
        ICreepTracker creepTracker,
        IGraphicalDebugger graphicalDebugger,
        IFrameClock frameClock
    ) {
        _visibilityTracker = visibilityTracker;
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _buildingTracker = buildingTracker;
        _regionsTracker = regionsTracker;
        _creepTracker = creepTracker;
        _graphicalDebugger = graphicalDebugger;
        _frameClock = frameClock;
    }

    protected override void ManagementPhase() {
        foreach (var creepTumor in _unitsTracker.GetUnits(_unitsTracker.NewOwnedUnits, Units.CreepTumor)) {
            TumorCreepSpreadModule.Install(creepTumor, _visibilityTracker, _terrainTracker, _buildingTracker, _creepTracker, _regionsTracker, _graphicalDebugger, _frameClock);
        }
    }

    public override string ToString() {
        return "CreepManager";
    }
}
