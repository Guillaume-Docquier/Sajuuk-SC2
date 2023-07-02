using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.GameSense;
using Sajuuk.MapAnalysis;
using SC2APIProtocol;

namespace Sajuuk.UnitModules;

public class UnitModuleInstaller : IUnitModuleInstaller {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IBuildingTracker _buildingTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly ICreepTracker _creepTracker;
    private readonly IPathfinder _pathfinder;
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IFrameClock _frameClock;

    public UnitModuleInstaller(
        IUnitsTracker unitsTracker,
        IGraphicalDebugger graphicalDebugger,
        IBuildingTracker buildingTracker,
        IRegionsTracker regionsTracker,
        ICreepTracker creepTracker,
        IPathfinder pathfinder,
        IVisibilityTracker visibilityTracker,
        ITerrainTracker terrainTracker,
        IFrameClock frameClock
    ) {
        _unitsTracker = unitsTracker;
        _graphicalDebugger = graphicalDebugger;
        _buildingTracker = buildingTracker;
        _regionsTracker = regionsTracker;
        _creepTracker = creepTracker;
        _pathfinder = pathfinder;
        _visibilityTracker = visibilityTracker;
        _terrainTracker = terrainTracker;
        _frameClock = frameClock;
    }

    public MiningModule InstallMiningModule(Unit worker, Unit assignedResource) {
        return Install(worker, new MiningModule(_graphicalDebugger, worker, assignedResource));
    }

    public AttackPriorityModule InstallAttackPriorityModule(Unit unit) {
        return Install(unit, new AttackPriorityModule(_unitsTracker, unit));
    }

    public CapacityModule InstallCapacityModule(Unit unit, int maxCapacity, bool showDebugInfo = true) {
        return Install(unit, new CapacityModule(_graphicalDebugger, unit, maxCapacity, showDebugInfo));
    }

    public ChangelingTargetingModule InstallChangelingTargetingModule(Unit unit) {
        return Install(unit, new ChangelingTargetingModule(_unitsTracker, unit));
    }

    public DebugLocationModule InstallDebugLocationModule(Unit unit, Color color = null, bool showName = false) {
        return Install(unit, new DebugLocationModule(_graphicalDebugger, unit, color, showName));
    }

    public QueenMicroModule InstallQueenMicroModule(Unit queen, Unit assignedTownHall) {
        return Install(queen, new QueenMicroModule(_buildingTracker, _regionsTracker, _creepTracker, _pathfinder, queen, assignedTownHall));
    }

    public TumorCreepSpreadModule InstallTumorCreepSpreadModule(Unit creepTumor) {
        return Install(creepTumor, new TumorCreepSpreadModule(_visibilityTracker, _terrainTracker, _buildingTracker, _creepTracker, _regionsTracker, _graphicalDebugger, _frameClock, creepTumor));
    }

    // The installation code should probably live within the Unit
    // However, it was not designed that way and since then I decided to avoid using unit modules
    // For this reason, I'll keep as much unit module code as possible outside of Unit
    private static TModule Install<TModule>(Unit unit, TModule module) where TModule : UnitModule {
        if (!CanInstall(unit, module)) {
            return null;
        }

        RemoveConflictingModules(unit, module);
        unit.Modules.Add(module.Tag, module);

        return module;
    }

    private static bool CanInstall(Unit unit, UnitModule unitModule) {
        if (unit == null) {
            Logger.Error($"Unit was null when trying to install {unitModule.Tag}.");

            return false;
        }

        return true;
    }

    private static void RemoveConflictingModules(Unit unit, UnitModule unitModule) {
        if (unit.Modules.Remove(unitModule.Tag)) {
            Logger.Warning($"Removed {unitModule.Tag} from {unit} because we're installing a new one.");
        }
    }
}
