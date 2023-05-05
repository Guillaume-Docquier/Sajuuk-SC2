using Bot.GameSense;
using Bot.Managers.ScoutManagement.ScoutingSupervision;
using Bot.Managers.ScoutManagement.ScoutingTasks;

namespace Bot.Managers.ScoutManagement;

public class ScoutSupervisorFactory : IScoutSupervisorFactory {
    private readonly IUnitsTracker _unitsTracker;

    public ScoutSupervisorFactory(IUnitsTracker unitsTracker) {
        _unitsTracker = unitsTracker;
    }

    public ScoutSupervisor CreateScoutSupervisor(ScoutingTask scoutingTask) {
        return new ScoutSupervisor(_unitsTracker, scoutingTask);
    }
}
