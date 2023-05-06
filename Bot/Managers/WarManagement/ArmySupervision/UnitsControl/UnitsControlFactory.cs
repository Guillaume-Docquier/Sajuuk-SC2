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

    public UnitsControlFactory(
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IGraphicalDebugger graphicalDebugger,
        IRegionsTracker regionsTracker,
        IRegionsEvaluationsTracker regionsEvaluationsTracker
    ) {
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _graphicalDebugger = graphicalDebugger;
        _regionsTracker = regionsTracker;
        _regionsEvaluationsTracker = regionsEvaluationsTracker;
    }

    public SneakAttack CreateSneakAttack() {
        return new SneakAttack(_unitsTracker, _terrainTracker, _graphicalDebugger);
    }

    public StutterStep CreateStutterStep() {
        return new StutterStep(_graphicalDebugger);
    }

    public BurrowHealing CreateBurrowHealing() {
        return new BurrowHealing(_unitsTracker, _terrainTracker, _regionsTracker, _regionsEvaluationsTracker);
    }

    public DefensiveUnitsControl CreateDefensiveUnitsControl() {
        return new DefensiveUnitsControl(this);
    }

    public DisengagementKiting CreateDisengagementKiting() {
        return new DisengagementKiting(_unitsTracker, _graphicalDebugger);
    }

    public MineralWalkKiting CreateMineralWalkKiting() {
        return new MineralWalkKiting(_unitsTracker, _terrainTracker, _graphicalDebugger);
    }

    public OffensiveUnitsControl CreateOffensiveUnitsControl() {
        return new OffensiveUnitsControl(this);
    }
}
