using System.Linq;
using SC2APIProtocol;

namespace Bot.GameSense.EnemyStrategyTracking;

public class EnemyStrategyTracker : INeedUpdating {
    public static readonly EnemyStrategyTracker Instance = new EnemyStrategyTracker();

    public EnemyStrategy EnemyStrategy { get; private set; }
    private IStrategyInterpreter _strategyInterpreter;

    private EnemyStrategyTracker() {}

    public void Reset() {
        _strategyInterpreter = null;
        EnemyStrategy = default;
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
            EnemyStrategy = _strategyInterpreter.Interpret(knownEnemyUnits);
        }
    }
}
