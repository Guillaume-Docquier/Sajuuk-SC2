using System.Collections.Generic;
using Sajuuk.Managers.ScoutManagement.ScoutingTasks;

namespace Sajuuk.Managers.ScoutManagement.ScoutingStrategies;

public interface IScoutingStrategy {
    IEnumerable<ScoutingTask> GetNextScoutingTasks();
}
