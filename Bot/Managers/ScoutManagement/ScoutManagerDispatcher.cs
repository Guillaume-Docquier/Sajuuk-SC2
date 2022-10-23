using System.Linq;
using Bot.ExtensionMethods;

namespace Bot.Managers.ScoutManagement;

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

            var ranks = supervisorsInNeed
                .OrderByDescending(supervisor => supervisor.ScoutingTask.Priority)
                .Select((supervisor, index) => (supervisor, rank: index + 1))
                .ToDictionary(tuple => tuple.supervisor, tuple => tuple.rank);


            var scoutingSupervisor = supervisorsInNeed
                .MinBy(supervisor => {
                    var distanceToTask = supervisor.ScoutingTask.ScoutLocation.HorizontalDistanceTo(unit);

                    var allowedScouts = supervisor.ScoutingTask.MaxScouts - supervisor.SupervisedUnits.Count;
                    var crowdMultiplier = allowedScouts <= 0 ? 2f : 1f / allowedScouts;

                    var rankMultiplier = ranks[supervisor] * ranks[supervisor];

                    return distanceToTask * crowdMultiplier * rankMultiplier;
                })!;

            scoutingSupervisor.Assign(unit);
        }
    }
}
