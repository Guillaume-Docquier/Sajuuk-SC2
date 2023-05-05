using Bot.Managers.ScoutManagement.ScoutingSupervision;
using Bot.Managers.ScoutManagement.ScoutingTasks;

namespace Bot.Managers.ScoutManagement;

public interface IScoutSupervisorFactory {
    public ScoutSupervisor CreateScoutSupervisor(ScoutingTask scoutingTask);
}
