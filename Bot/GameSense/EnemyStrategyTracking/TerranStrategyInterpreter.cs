using System.Collections.Generic;

namespace Bot.GameSense.EnemyStrategyTracking;

public class TerranStrategyInterpreter : IStrategyInterpreter {
    public EnemyStrategy Interpret(List<Unit> enemyUnits) {
        return EnemyStrategy.Unknown;
    }
}
