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
    private readonly IUnitsTracker _unitsTracker;

    private bool _isScenarioDone = false;

    public SpawnStuffScenario(IUnitsTracker unitsTracker) {
        _unitsTracker = unitsTracker;
    }

    public async Task OnFrame() {
        if (_isScenarioDone) {
            return;
        }

        if (Controller.Frame >= TimeUtils.SecsToFrames(50)) {
            var main = Controller.GetUnits(_unitsTracker.OwnedUnits, Units.TownHalls)
                .MaxBy(townHall => Pathfinder.FindPath(townHall.Position.ToVector2(), MapAnalyzer.EnemyStartingLocation).Count);

            Logger.Debug("Spawning 1 probe on the main");

            await Program.GameConnection.SendRequest(RequestBuilder.DebugCreateUnit(Owner.Enemy, Units.Probe, 3, main!.Position));
            Controller.SetRealTime("SpawnStuffScenario");

            _isScenarioDone = true;
        }
    }
}
