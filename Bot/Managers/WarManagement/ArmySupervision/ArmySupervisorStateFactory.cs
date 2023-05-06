using Bot.Debugging.GraphicalDebugging;
using Bot.GameSense;
using Bot.GameSense.RegionsEvaluationsTracking;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl;

namespace Bot.Managers.WarManagement.ArmySupervision;

public class ArmySupervisorStateFactory : IArmySupervisorStateFactory {
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly IRegionsEvaluationsTracker _regionsEvaluationsTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IUnitsControlFactory _unitsControlFactory;

    public ArmySupervisorStateFactory(
        IVisibilityTracker visibilityTracker,
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IRegionsTracker regionsTracker,
        IRegionsEvaluationsTracker regionsEvaluationsTracker,
        IGraphicalDebugger graphicalDebugger,
        IUnitsControlFactory unitsControlFactory
    ) {
        _visibilityTracker = visibilityTracker;
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _regionsTracker = regionsTracker;
        _regionsEvaluationsTracker = regionsEvaluationsTracker;
        _graphicalDebugger = graphicalDebugger;
        _unitsControlFactory = unitsControlFactory;
    }

    public ArmySupervisor.AttackState CreateAttackState() {
        return new ArmySupervisor.AttackState(_unitsTracker, _terrainTracker, _graphicalDebugger, this, _unitsControlFactory);
    }

    public ArmySupervisor.DefenseState CreateDefenseState() {
        return new ArmySupervisor.DefenseState(_unitsTracker, _terrainTracker, _graphicalDebugger, this, _unitsControlFactory);
    }

    public ArmySupervisor.HuntState CreateHuntState() {
        return new ArmySupervisor.HuntState(_visibilityTracker, _unitsTracker, _terrainTracker, _regionsTracker, this);
    }

    public ArmySupervisor.RallyState CreateRallyState() {
        return new ArmySupervisor.RallyState(_terrainTracker, _graphicalDebugger, this);
    }
}
