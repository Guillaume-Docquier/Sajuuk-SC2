using System.Collections.Generic;

namespace Sajuuk.GameSense.EnemyStrategyTracking.StrategyInterpretation;

public class ProtossStrategyInterpreter : IStrategyInterpreter {
    public EnemyStrategy Interpret(List<Unit> enemyUnits) {
        return EnemyStrategy.Unknown;
    }
}
