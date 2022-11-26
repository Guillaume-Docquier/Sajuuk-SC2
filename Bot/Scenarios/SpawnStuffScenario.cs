using System.Linq;
using System.Threading.Tasks;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapKnowledge;
using Bot.Utils;
using Bot.Wrapper;

namespace Bot.Scenarios;

public class SpawnStuffScenario : IScenario {
    private bool _isScenarioDone = false;

    public async Task OnFrame() {
        if (_isScenarioDone) {
            return;
        }

        if (Controller.Frame >= TimeUtils.SecsToFrames(50)) {
            var main = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.TownHalls)
                .MaxBy(townHall => Pathfinder.FindPath(townHall.Position.ToVector2(), MapAnalyzer.EnemyStartingLocation).Count);

            Logger.Debug("Spawning 1 probe on the main");

            await Program.GameConnection.SendRequest(RequestBuilder.DebugCreateUnit(Owner.Enemy, Units.Probe, 3, main!.Position));
            Controller.SetRealTime("SpawnStuffScenario");

            _isScenarioDone = true;
        }
    }
}
