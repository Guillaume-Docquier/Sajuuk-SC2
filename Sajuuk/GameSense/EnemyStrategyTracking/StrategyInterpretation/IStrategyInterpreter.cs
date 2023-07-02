using System.Collections.Generic;

namespace Sajuuk.GameSense.EnemyStrategyTracking.StrategyInterpretation;

public interface IStrategyInterpreter {
    EnemyStrategy Interpret(List<Unit> enemyUnits);
}
