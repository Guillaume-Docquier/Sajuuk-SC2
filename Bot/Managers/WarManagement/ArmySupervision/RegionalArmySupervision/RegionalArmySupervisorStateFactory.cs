using Bot.GameSense;
using Bot.GameSense.RegionsEvaluationsTracking;
using Bot.Managers.WarManagement.ArmySupervision.UnitsControl;

namespace Bot.Managers.WarManagement.ArmySupervision.RegionalArmySupervision;

public class RegionalArmySupervisorStateFactory : IRegionalArmySupervisorStateFactory {
    private readonly IRegionsTracker _regionsTracker;
    private readonly IRegionsEvaluationsTracker _regionsEvaluationsTracker;
    private readonly IUnitsControlFactory _unitsControlFactory;

    public RegionalArmySupervisorStateFactory(
        IRegionsTracker regionsTracker,
        IRegionsEvaluationsTracker regionsEvaluationsTracker,
        IUnitsControlFactory unitsControlFactory
    ) {
        _regionsTracker = regionsTracker;
        _regionsEvaluationsTracker = regionsEvaluationsTracker;
        _unitsControlFactory = unitsControlFactory;
    }

    public ApproachState CreateApproachState() {
        return new ApproachState(_regionsTracker, _regionsEvaluationsTracker, this);
    }

    public DisengageState CreateDisengageState() {
        return new DisengageState(_regionsTracker, _regionsEvaluationsTracker, _unitsControlFactory, this);
    }

    public EngageState CreateEngageState() {
        return new EngageState(_regionsTracker, _regionsEvaluationsTracker, this);
    }
}
