using Sajuuk.Managers.ScoutManagement.ScoutingSupervision;
using Sajuuk.Managers.ScoutManagement.ScoutingTasks;

namespace Sajuuk.Managers.ScoutManagement;

public interface IScoutSupervisorFactory {
    public ScoutSupervisor CreateScoutSupervisor(ScoutingTask scoutingTask);
}
