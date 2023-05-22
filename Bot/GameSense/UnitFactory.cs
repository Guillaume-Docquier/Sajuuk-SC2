using Bot.GameData;
using Bot.Wrapper;

namespace Bot.GameSense;

public class UnitFactory : IUnitFactory {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IFrameClock _frameClock;
    private readonly KnowledgeBase _knowledgeBase;
    private readonly IActionBuilder _actionBuilder;
    private readonly IActionService _actionService;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IRegionsTracker _regionsTracker;

    public UnitFactory(
        IFrameClock frameClock,
        KnowledgeBase knowledgeBase,
        IActionBuilder actionBuilder,
        IActionService actionService,
        ITerrainTracker terrainTracker,
        IRegionsTracker regionsTracker,
        IUnitsTracker unitsTracker
    ) {
        _frameClock = frameClock;
        _knowledgeBase = knowledgeBase;
        _actionBuilder = actionBuilder;
        _actionService = actionService;
        _terrainTracker = terrainTracker;
        _regionsTracker = regionsTracker;
        _unitsTracker = unitsTracker;
    }

    public Unit CreateUnit(SC2APIProtocol.Unit rawUnit, ulong frame) {
        return new Unit(_frameClock, _knowledgeBase, _actionBuilder, _actionService, _terrainTracker, _regionsTracker, _unitsTracker, rawUnit, frame);
    }
}
