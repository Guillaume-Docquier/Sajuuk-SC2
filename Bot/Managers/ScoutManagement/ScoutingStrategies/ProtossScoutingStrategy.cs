using System.Collections.Generic;
using System.Linq;
using Bot.Managers.ScoutManagement.ScoutingTasks;

namespace Bot.Managers.ScoutManagement.ScoutingStrategies;

public class ProtossScoutingStrategy : IScoutingStrategy {
    public IEnumerable<ScoutingTask> Execute() {
        return Enumerable.Empty<ScoutingTask>();
    }
}
