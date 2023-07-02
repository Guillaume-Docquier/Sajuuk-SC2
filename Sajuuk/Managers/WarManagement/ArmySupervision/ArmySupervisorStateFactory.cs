using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.GameSense;
using Sajuuk.GameSense.RegionsEvaluationsTracking;
using Sajuuk.Managers.WarManagement.ArmySupervision.UnitsControl;
using Sajuuk.MapAnalysis;

namespace Sajuuk.Managers.WarManagement.ArmySupervision;

public class ArmySupervisorStateFactory : IArmySupervisorStateFactory {
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly IRegionsEvaluationsTracker _regionsEvaluationsTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IUnitsControlFactory _unitsControlFactory;
    private readonly IFrameClock _frameClock;
    private readonly IController _controller;
    private readonly IUnitEvaluator _unitEvaluator;
    private readonly IPathfinder _pathfinder;

    public ArmySupervisorStateFactory(
        IVisibilityTracker visibilityTracker,
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IRegionsTracker regionsTracker,
        IRegionsEvaluationsTracker regionsEvaluationsTracker,
        IGraphicalDebugger graphicalDebugger,
        IUnitsControlFactory unitsControlFactory,
        IFrameClock frameClock,
        IController controller,
        IUnitEvaluator unitEvaluator,
        IPathfinder pathfinder
    ) {
        _visibilityTracker = visibilityTracker;
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _regionsTracker = regionsTracker;
        _regionsEvaluationsTracker = regionsEvaluationsTracker;
        _graphicalDebugger = graphicalDebugger;
        _unitsControlFactory = unitsControlFactory;
        _frameClock = frameClock;
        _controller = controller;
        _unitEvaluator = unitEvaluator;
        _pathfinder = pathfinder;
    }

    public ArmySupervisor.AttackState CreateAttackState() {
        return new ArmySupervisor.AttackState(_unitsTracker, _terrainTracker, _graphicalDebugger, this, _unitsControlFactory, _frameClock, _unitEvaluator, _pathfinder);
    }

    public ArmySupervisor.DefenseState CreateDefenseState() {
        return new ArmySupervisor.DefenseState(_unitsTracker, _terrainTracker, _graphicalDebugger, this, _unitsControlFactory, _unitEvaluator);
    }

    public ArmySupervisor.HuntState CreateHuntState() {
        return new ArmySupervisor.HuntState(_visibilityTracker, _unitsTracker, _terrainTracker, _regionsTracker, this, _pathfinder);
    }

    public ArmySupervisor.RallyState CreateRallyState() {
        return new ArmySupervisor.RallyState(_terrainTracker, _graphicalDebugger, this, _controller, _unitEvaluator);
    }
}
