using System.Linq;
using Sajuuk.ExtensionMethods;

namespace Sajuuk.Managers.ScoutManagement;

public partial class ScoutManager {
    private class ScoutManagerDispatcher : Dispatcher<ScoutManager> {
        public ScoutManagerDispatcher(ScoutManager client) : base(client) {}

        public override void Dispatch(Unit unit) {
            var supervisorsInNeed = Client._scoutSupervisors
                .Where(supervisor => supervisor.ScoutingTask.MaxScouts > supervisor.SupervisedUnits.Count)
                .ToList();

            if (supervisorsInNeed.Count <= 0) {
                return;
            }

            supervisorsInNeed
                .OrderBy(supervisor => supervisor.SupervisedUnits.Count)
                .ThenByDescending(supervisor => supervisor.ScoutingTask.Priority)
                .ThenBy(supervisor => supervisor.ScoutingTask.ScoutLocation.DistanceTo(unit))
                .First()
                .Assign(unit);
        }
    }
}
