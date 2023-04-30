using System.Linq;
using System.Threading.Tasks;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapAnalysis;
using Bot.Utils;
using Bot.Wrapper;

namespace Bot.Scenarios;

public class SpawnStuffScenario : IScenario {
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;

    private bool _isScenarioDone = false;

    public SpawnStuffScenario(IUnitsTracker unitsTracker, ITerrainTracker terrainTracker) {
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
    }

    public async Task OnFrame() {
        if (_isScenarioDone) {
            return;
        }

        if (Controller.Frame >= TimeUtils.SecsToFrames(50)) {
            var main = Controller.GetUnits(_unitsTracker.OwnedUnits, Units.TownHalls)
                .MaxBy(townHall => Pathfinder.Instance.FindPath(townHall.Position.ToVector2(), _terrainTracker.EnemyStartingLocation).Count);

            Logger.Debug("Spawning 1 probe on the main");

            await Program.GameConnection.SendRequest(RequestBuilder.DebugCreateUnit(Owner.Enemy, Units.Probe, 3, main!.Position));
            Controller.SetRealTime("SpawnStuffScenario");

            _isScenarioDone = true;
        }
    }
}
