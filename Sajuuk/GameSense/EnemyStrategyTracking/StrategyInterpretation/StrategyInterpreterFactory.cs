using Sajuuk.GameData;
using SC2APIProtocol;

namespace Sajuuk.GameSense.EnemyStrategyTracking.StrategyInterpretation;

public class StrategyInterpreterFactory : IStrategyInterpreterFactory {
    private readonly IFrameClock _frameClock;
    private readonly KnowledgeBase _knowledgeBase;
    private readonly IEnemyRaceTracker _enemyRaceTracker;
    private readonly IRegionsTracker _regionsTracker;

    public StrategyInterpreterFactory(
        IFrameClock frameClock,
        KnowledgeBase knowledgeBase,
        IEnemyRaceTracker enemyRaceTracker,
        IRegionsTracker regionsTracker
    ) {
        _frameClock = frameClock;
        _knowledgeBase = knowledgeBase;
        _enemyRaceTracker = enemyRaceTracker;
        _regionsTracker = regionsTracker;
    }

    public IStrategyInterpreter CreateNew() {
        return _enemyRaceTracker.EnemyRace switch
        {
            Race.Terran => new TerranStrategyInterpreter(),
            Race.Zerg => new ZergStrategyInterpreter(_frameClock, _knowledgeBase, _regionsTracker),
            Race.Protoss => new ProtossStrategyInterpreter(),
            _ => null,
        };
    }
}
