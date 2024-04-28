using Sajuuk.GameData;

namespace Sajuuk.GameSense;

public class UnitFactory : IUnitFactory {
    private readonly IUnitsTracker _unitsTracker;
    private readonly IFrameClock _frameClock;
    private readonly KnowledgeBase _knowledgeBase;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IRegionsTracker _regionsTracker;

    public UnitFactory(
        IFrameClock frameClock,
        KnowledgeBase knowledgeBase,
        ITerrainTracker terrainTracker,
        IRegionsTracker regionsTracker,
        IUnitsTracker unitsTracker
    ) {
        _frameClock = frameClock;
        _knowledgeBase = knowledgeBase;
        _terrainTracker = terrainTracker;
        _regionsTracker = regionsTracker;
        _unitsTracker = unitsTracker;
    }

    public Unit CreateUnit(SC2APIProtocol.Unit rawUnit, ulong frame) {
        return new Unit(_frameClock, _knowledgeBase, _terrainTracker, _regionsTracker, _unitsTracker, rawUnit, frame);
    }
}
