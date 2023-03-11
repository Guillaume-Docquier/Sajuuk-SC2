using System.Collections.Generic;
using Bot.Managers.ScoutManagement.ScoutingTasks;

namespace Bot.Managers.ScoutManagement.ScoutingStrategies;

public interface IScoutingStrategy {
    IEnumerable<ScoutingTask> GetNextScoutingTasks();
}
