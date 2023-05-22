using Bot.MapAnalysis;
using SC2APIProtocol;

namespace Bot.GameSense.RegionsEvaluationsTracking.RegionsEvaluations;

public class RegionsEvaluatorFactory : IRegionsEvaluatorFactory {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IFrameClock _frameClock;
    private readonly IUnitEvaluator _unitEvaluator;
    private readonly IPathfinder _pathfinder;

    public RegionsEvaluatorFactory(
        IUnitsTracker unitsTracker,
        IFrameClock frameClock,
        IUnitEvaluator unitEvaluator,
        IPathfinder pathfinder
    ) {
        _unitsTracker = unitsTracker;
        _frameClock = frameClock;
        _unitEvaluator = unitEvaluator;
        _pathfinder = pathfinder;
    }

    public RegionsThreatEvaluator CreateRegionsThreatEvaluator(RegionsForceEvaluator enemyForceEvaluator, RegionsValueEvaluator selfValueEvaluator) {
        return new RegionsThreatEvaluator(_frameClock, _pathfinder, enemyForceEvaluator, selfValueEvaluator);
    }

    public RegionsForceEvaluator CreateRegionsForceEvaluator(Alliance alliance) {
        return new RegionsForceEvaluator(_unitsTracker, _frameClock, _unitEvaluator, alliance);
    }

    public RegionsValueEvaluator CreateRegionsValueEvaluator(Alliance alliance) {
        return new RegionsValueEvaluator(_unitsTracker, _frameClock, _unitEvaluator, alliance);
    }
}
