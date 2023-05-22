using System.Collections.Generic;
using System.Linq;
using Bot.GameSense.EnemyStrategyTracking.StrategyInterpretation;
using Bot.Tagging;
using SC2APIProtocol;

namespace Bot.GameSense.EnemyStrategyTracking;

public class EnemyStrategyTracker : IEnemyStrategyTracker, INeedUpdating {
    private readonly ITaggingService _taggingService;
    private readonly IUnitsTracker _unitsTracker;
    private readonly IStrategyInterpreterFactory _strategyInterpreterFactory;

    private readonly HashSet<ISubscriber<EnemyStrategyTransition>> _subscribers = new HashSet<ISubscriber<EnemyStrategyTransition>>();

    private IStrategyInterpreter _strategyInterpreter;

    public EnemyStrategy CurrentEnemyStrategy { get; private set; } = EnemyStrategy.Unknown;

    public EnemyStrategyTracker(
        ITaggingService taggingService,
        IUnitsTracker unitsTracker,
        IStrategyInterpreterFactory strategyInterpreterFactory
    ) {
        _taggingService = taggingService;
        _unitsTracker = unitsTracker;
        _strategyInterpreterFactory = strategyInterpreterFactory;
    }

    public void Update(ResponseObservation observation, ResponseGameInfo gameInfo) {
        _strategyInterpreter ??= _strategyInterpreterFactory.CreateNew();
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
