using System.Linq;
using SC2APIProtocol;

namespace Bot.GameSense.EnemyStrategyTracking;

public class EnemyStrategyTracker : INeedUpdating {
    public static EnemyStrategyTracker Instance { get; private set; } = new EnemyStrategyTracker();

    private EnemyStrategy _enemyStrategy = EnemyStrategy.Unknown;
    private IStrategyInterpreter _strategyInterpreter;

    public static EnemyStrategy EnemyStrategy => Instance._enemyStrategy;

    private EnemyStrategyTracker() {}

    public void Reset() {
        Instance = new EnemyStrategyTracker();
    }

    public void Update(ResponseObservation observation) {
        _strategyInterpreter ??= Controller.EnemyRace switch
        {
            Race.Terran => new TerranStrategyInterpreter(),
            Race.Zerg => new ZergStrategyInterpreter(),
            Race.Protoss => new ProtossStrategyInterpreter(),
            _ => null,
        };

        if (_strategyInterpreter != null) {
            var knownEnemyUnits = UnitsTracker.EnemyUnits.Concat(UnitsTracker.EnemyMemorizedUnits.Values).ToList();
            var newEnemyStrategy = _strategyInterpreter.Interpret(knownEnemyUnits);
            if (_enemyStrategy != newEnemyStrategy) {
                _enemyStrategy = newEnemyStrategy;
                Controller.TagGame($"Strategy_{_enemyStrategy}_{Controller.GetGameTimeString()}");
            }
        }
    }
}
