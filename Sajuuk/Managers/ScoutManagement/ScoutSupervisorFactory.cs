using Sajuuk.GameSense;
using Sajuuk.Managers.ScoutManagement.ScoutingSupervision;
using Sajuuk.Managers.ScoutManagement.ScoutingTasks;

namespace Sajuuk.Managers.ScoutManagement;

public class ScoutSupervisorFactory : IScoutSupervisorFactory {
    private readonly IUnitsTracker _unitsTracker;

    public ScoutSupervisorFactory(IUnitsTracker unitsTracker) {
        _unitsTracker = unitsTracker;
    }

    public ScoutSupervisor CreateScoutSupervisor(ScoutingTask scoutingTask) {
        return new ScoutSupervisor(_unitsTracker, scoutingTask);
    }
}
