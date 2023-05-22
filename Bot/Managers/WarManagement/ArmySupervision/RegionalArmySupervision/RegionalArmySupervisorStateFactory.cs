using Bot.GameSense;
using Bot.GameSense.RegionsEvaluationsTracking;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl;
using Bot.MapAnalysis;

namespace Bot.Managers.WarManagement.ArmySupervision.RegionalArmySupervision;

public class RegionalArmySupervisorStateFactory : IRegionalArmySupervisorStateFactory {
    private readonly IRegionsTracker _regionsTracker;
    private readonly IRegionsEvaluationsTracker _regionsEvaluationsTracker;
    private readonly IUnitsControlFactory _unitsControlFactory;
    private readonly IUnitEvaluator _unitEvaluator;
    private readonly IPathfinder _pathfinder;

    public RegionalArmySupervisorStateFactory(
        IRegionsTracker regionsTracker,
        IRegionsEvaluationsTracker regionsEvaluationsTracker,
        IUnitsControlFactory unitsControlFactory,
        IUnitEvaluator unitEvaluator,
        IPathfinder pathfinder
    ) {
        _regionsTracker = regionsTracker;
        _regionsEvaluationsTracker = regionsEvaluationsTracker;
        _unitsControlFactory = unitsControlFactory;
        _unitEvaluator = unitEvaluator;
        _pathfinder = pathfinder;
    }

    public ApproachState CreateApproachState() {
        return new ApproachState(_regionsTracker, _regionsEvaluationsTracker, this, _unitEvaluator, _pathfinder);
    }

    public DisengageState CreateDisengageState() {
        return new DisengageState(_regionsTracker, _regionsEvaluationsTracker, _unitsControlFactory, this, _pathfinder);
    }

    public EngageState CreateEngageState() {
        return new EngageState(_regionsTracker, _regionsEvaluationsTracker, this, _unitEvaluator, _pathfinder);
    }
}
