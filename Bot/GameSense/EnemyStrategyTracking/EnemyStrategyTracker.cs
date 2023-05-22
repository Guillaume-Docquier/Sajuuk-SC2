using System.Collections.Generic;
using System.Linq;
using Bot.GameData;
using Bot.Tagging;
using SC2APIProtocol;

namespace Bot.GameSense.EnemyStrategyTracking;

public class EnemyStrategyTracker : IEnemyStrategyTracker, INeedUpdating {
    private readonly ITaggingService _taggingService;
    private readonly IEnemyRaceTracker _enemyRaceTracker;
    private readonly IUnitsTracker _unitsTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly KnowledgeBase _knowledgeBase;
    private readonly IFrameClock _frameClock;

    private readonly HashSet<ISubscriber<EnemyStrategyTransition>> _subscribers = new HashSet<ISubscriber<EnemyStrategyTransition>>();

    private IStrategyInterpreter _strategyInterpreter;

    public EnemyStrategy CurrentEnemyStrategy { get; private set; } = EnemyStrategy.Unknown;

    public EnemyStrategyTracker(
        ITaggingService taggingService,
        IEnemyRaceTracker enemyRaceTracker,
        IUnitsTracker unitsTracker,
        IRegionsTracker regionsTracker,
        KnowledgeBase knowledgeBase,
        IFrameClock frameClock
    ) {
        _taggingService = taggingService;
        _enemyRaceTracker = enemyRaceTracker;
        _unitsTracker = unitsTracker;
        _regionsTracker = regionsTracker;
        _knowledgeBase = knowledgeBase;
        _frameClock = frameClock;
    }

    public void Reset() {
        _subscribers.Clear();
        _strategyInterpreter = null;
        CurrentEnemyStrategy = EnemyStrategy.Unknown;
    }

    public void Update(ResponseObservation observation, ResponseGameInfo gameInfo) {
        _strategyInterpreter ??= _enemyRaceTracker.EnemyRace switch
        {
            Race.Terran => new TerranStrategyInterpreter(),
            Race.Zerg => new ZergStrategyInterpreter(_regionsTracker, _knowledgeBase, _frameClock),
            Race.Protoss => new ProtossStrategyInterpreter(),
            _ => null,
        };

        if (_strategyInterpreter == null) {
            return;
        }

        var knownEnemyUnits = _unitsTracker.EnemyUnits.Concat(_unitsTracker.EnemyMemorizedUnits.Values).ToList();
        var newEnemyStrategy = _strategyInterpreter.Interpret(knownEnemyUnits);
        if (newEnemyStrategy == EnemyStrategy.Unknown || CurrentEnemyStrategy == newEnemyStrategy) {
            return;
        }

        var transition = new EnemyStrategyTransition
        {
            PreviousStrategy = CurrentEnemyStrategy,
            CurrentStrategy = newEnemyStrategy
        };

        CurrentEnemyStrategy = newEnemyStrategy;
        _taggingService.TagEnemyStrategy(CurrentEnemyStrategy.ToString());

        NotifyOfStrategyChanged(transition);
    }

    private void NotifyOfStrategyChanged(EnemyStrategyTransition transition) {
        foreach (var subscriber in _subscribers) {
            subscriber.Notify(transition);
        }
    }

    public void Register(ISubscriber<EnemyStrategyTransition> subscriber) {
        _subscribers.Add(subscriber);
    }

    public void UnRegister(ISubscriber<EnemyStrategyTransition> subscriber) {
        _subscribers.Remove(subscriber);
    }
}
