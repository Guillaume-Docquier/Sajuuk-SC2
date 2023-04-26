using System.Collections.Generic;
using System.Linq;
using Bot.MapKnowledge;
using Bot.Tagging;
using SC2APIProtocol;

namespace Bot.GameSense.EnemyStrategyTracking;

public class EnemyStrategyTracker : INeedUpdating, IPublisher<EnemyStrategyTransition> {
    public static EnemyStrategyTracker Instance { get; private set; } = new EnemyStrategyTracker(
        TaggingService.Instance,
        EnemyRaceTracker.Instance,
        UnitsTracker.Instance,
        ExpandAnalyzer.Instance,
        RegionAnalyzer.Instance
    );

    private readonly ITaggingService _taggingService;
    private readonly IEnemyRaceTracker _enemyRaceTracker;
    private readonly IUnitsTracker _unitsTracker;
    private readonly IExpandAnalyzer _expandAnalyzer;
    private readonly IRegionAnalyzer _regionAnalyzer;

    private readonly HashSet<ISubscriber<EnemyStrategyTransition>> _subscribers = new HashSet<ISubscriber<EnemyStrategyTransition>>();

    private IStrategyInterpreter _strategyInterpreter;

    public EnemyStrategy CurrentEnemyStrategy { get; private set; } = EnemyStrategy.Unknown;

    private EnemyStrategyTracker(
        ITaggingService taggingService,
        IEnemyRaceTracker enemyRaceTracker,
        IUnitsTracker unitsTracker,
        IExpandAnalyzer expandAnalyzer,
        IRegionAnalyzer regionAnalyzer
    ) {
        _taggingService = taggingService;
        _enemyRaceTracker = enemyRaceTracker;
        _unitsTracker = unitsTracker;
        _expandAnalyzer = expandAnalyzer;
        _regionAnalyzer = regionAnalyzer;
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
            Race.Zerg => new ZergStrategyInterpreter(_expandAnalyzer, _regionAnalyzer),
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
