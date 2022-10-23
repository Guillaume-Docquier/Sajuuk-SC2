using System.Collections.Generic;
using System.Linq;
using Bot.Managers.ScoutManagement.ScoutingTasks;

namespace Bot.Managers.ScoutManagement.ScoutingStrategies;

public class TerranScoutingStrategy : IScoutingStrategy {
    public IEnumerable<ScoutingTask> Execute() {
        return Enumerable.Empty<ScoutingTask>();
    }
}
