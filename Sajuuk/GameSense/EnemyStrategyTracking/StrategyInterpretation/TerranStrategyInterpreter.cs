using System.Collections.Generic;

namespace Sajuuk.GameSense.EnemyStrategyTracking.StrategyInterpretation;

public class TerranStrategyInterpreter : IStrategyInterpreter {
    public EnemyStrategy Interpret(List<Unit> enemyUnits) {
        return EnemyStrategy.Unknown;
    }
}
