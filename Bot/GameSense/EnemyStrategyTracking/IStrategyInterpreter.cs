using System.Collections.Generic;

namespace Bot.GameSense.EnemyStrategyTracking;

public interface IStrategyInterpreter {
    EnemyStrategy Interpret(List<Unit> enemyUnits);
}
