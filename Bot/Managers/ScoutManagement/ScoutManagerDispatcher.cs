using System.Linq;
using Bot.ExtensionMethods;

namespace Bot.Managers.ScoutManagement;

public partial class ScoutManager {
    private class ScoutManagerDispatcher : Dispatcher<ScoutManager> {
        public ScoutManagerDispatcher(ScoutManager client) : base(client) {}

        public override void Dispatch(Unit unit) {
            if (Client._scoutSupervisors.Count <= 0) {
                return;
            }

            // TODO GD Bucket by priority?
            var scoutingSupervisor = Client._scoutSupervisors.MinBy(supervisor => {
                var crowdMultiplier = 1 + supervisor.SupervisedUnits.Count;
                var distanceToTask = supervisor.ScoutingTask.ScoutLocation.HorizontalDistanceTo(unit);

                return crowdMultiplier * distanceToTask;
            })!;

            scoutingSupervisor.Assign(unit);
        }
    }
}
