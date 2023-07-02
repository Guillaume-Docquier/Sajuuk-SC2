using Sajuuk.Algorithms;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.GameSense;
using Sajuuk.Managers.WarManagement.ArmySupervision;
using Sajuuk.Managers.WarManagement.ArmySupervision.RegionalArmySupervision;
using Sajuuk.Managers.WarManagement.ArmySupervision.UnitsControl;
using Sajuuk.MapAnalysis.RegionAnalysis;

namespace Sajuuk.Managers.WarManagement;

public class WarSupervisorFactory : IWarSupervisorFactory {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IArmySupervisorStateFactory _armySupervisorStateFactory;
    private readonly IUnitsControlFactory _unitsControlFactory;
    private readonly IRegionalArmySupervisorStateFactory _regionalArmySupervisorStateFactory;
    private readonly IClustering _clustering;
    private readonly IUnitEvaluator _unitEvaluator;

    public WarSupervisorFactory(
        IUnitsTracker unitsTracker,
        IGraphicalDebugger graphicalDebugger,
        IArmySupervisorStateFactory armySupervisorStateFactory,
        IUnitsControlFactory unitsControlFactory,
        IRegionalArmySupervisorStateFactory regionalArmySupervisorStateFactory,
        IClustering clustering,
        IUnitEvaluator unitEvaluator
    ) {
        _unitsTracker = unitsTracker;
        _graphicalDebugger = graphicalDebugger;
        _armySupervisorStateFactory = armySupervisorStateFactory;
        _unitsControlFactory = unitsControlFactory;
        _regionalArmySupervisorStateFactory = regionalArmySupervisorStateFactory;
        _clustering = clustering;
        _unitEvaluator = unitEvaluator;
    }

    public ArmySupervisor CreateArmySupervisor() {
        return new ArmySupervisor(_armySupervisorStateFactory, _clustering, _unitEvaluator);
    }

    public RegionalArmySupervisor CreateRegionalArmySupervisor(IRegion region) {
        return new RegionalArmySupervisor(_unitsTracker, _graphicalDebugger, _unitsControlFactory, _regionalArmySupervisorStateFactory, _clustering, region);
    }
}
