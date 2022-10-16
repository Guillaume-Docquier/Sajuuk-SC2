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

            var scoutingSupervisor = Client._scoutSupervisors.MinBy(supervisor => supervisor.SupervisedUnits.Count)!;
            scoutingSupervisor.Assign(unit);
        }
    }
}
