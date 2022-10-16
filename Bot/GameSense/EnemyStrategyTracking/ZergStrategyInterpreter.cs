using System.Collections.Generic;

namespace Bot.GameSense.EnemyStrategyTracking;

public class ZergStrategyInterpreter : IStrategyInterpreter {
    public EnemyStrategy Interpret(List<Unit> enemyUnits) {
        return EnemyStrategy.Unknown;
    }
}
