using System.Collections.Generic;

namespace Bot.GameSense.EnemyStrategyTracking.StrategyInterpretation;

public interface IStrategyInterpreter {
    EnemyStrategy Interpret(List<Unit> enemyUnits);
}
