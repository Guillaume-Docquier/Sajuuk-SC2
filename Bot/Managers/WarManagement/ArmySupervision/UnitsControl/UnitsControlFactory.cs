using Bot.Algorithms;
using Bot.Debugging.GraphicalDebugging;
using Bot.GameSense;
using Bot.GameSense.RegionsEvaluationsTracking;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl.SneakAttackUnitsControl;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl.StutterStepUnitsControl;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl;

public class UnitsControlFactory : IUnitsControlFactory {
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IRegionsTracker _regionsTracker;
    private readonly IRegionsEvaluationsTracker _regionsEvaluationsTracker;
    private readonly IFrameClock _frameClock;
    private readonly IController _controller;
    private readonly IDetectionTracker _detectionTracker;
    private readonly IUnitEvaluator _unitEvaluator;
    private readonly IClustering _clustering;
    private readonly ISneakAttackStateFactory _sneakAttackStateFactory;

    public UnitsControlFactory(
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IGraphicalDebugger graphicalDebugger,
        IRegionsTracker regionsTracker,
        IRegionsEvaluationsTracker regionsEvaluationsTracker,
        IFrameClock frameClock,
        IController controller,
        IDetectionTracker detectionTracker,
        IUnitEvaluator unitEvaluator,
        IClustering clustering,
        ISneakAttackStateFactory sneakAttackStateFactory
    ) {
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _graphicalDebugger = graphicalDebugger;
        _regionsTracker = regionsTracker;
        _regionsEvaluationsTracker = regionsEvaluationsTracker;
        _frameClock = frameClock;
        _controller = controller;
        _detectionTracker = detectionTracker;
        _unitEvaluator = unitEvaluator;
        _clustering = clustering;
        _sneakAttackStateFactory = sneakAttackStateFactory;
    }

    public SneakAttack CreateSneakAttack() {
        return new SneakAttack(_unitsTracker, _terrainTracker, _graphicalDebugger, _frameClock, _controller, _detectionTracker, _sneakAttackStateFactory);
    }

    public StutterStep CreateStutterStep() {
        return new StutterStep(_graphicalDebugger);
    }

    public BurrowHealing CreateBurrowHealing() {
        return new BurrowHealing(_unitsTracker, _terrainTracker, _regionsTracker, _regionsEvaluationsTracker, _controller, _detectionTracker);
    }

    public DefensiveUnitsControl CreateDefensiveUnitsControl() {
        return new DefensiveUnitsControl(this);
    }

    public DisengagementKiting CreateDisengagementKiting() {
        return new DisengagementKiting(_unitsTracker, _graphicalDebugger);
    }

    public MineralWalkKiting CreateMineralWalkKiting() {
        return new MineralWalkKiting(_unitsTracker, _terrainTracker, _graphicalDebugger, _clustering);
    }

    public OffensiveUnitsControl CreateOffensiveUnitsControl() {
        return new OffensiveUnitsControl(this);
    }
}
