using System.Linq;
using System.Threading.Tasks;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapAnalysis;
using Bot.Requests;
using Bot.Utils;

namespace Bot.Scenarios;

public class SpawnStuffScenario : IScenario {
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IFrameClock _frameClock;
    private readonly IController _controller;
    private readonly IPathfinder _pathfinder;
    private readonly IRequestBuilder _requestBuilder;
    private readonly IRequestService _requestService;

    private bool _isScenarioDone = false;

    public SpawnStuffScenario(
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IFrameClock frameClock,
        IController controller,
        IPathfinder pathfinder,
        IRequestBuilder requestBuilder,
        IRequestService requestService
    ) {
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _frameClock = frameClock;
        _controller = controller;
        _pathfinder = pathfinder;
        _requestBuilder = requestBuilder;
        _requestService = requestService;
    }

    public async Task OnFrame() {
        if (_isScenarioDone) {
            return;
        }

        if (_frameClock.CurrentFrame >= TimeUtils.SecsToFrames(50)) {
            var main = _unitsTracker.GetUnits(_unitsTracker.OwnedUnits, Units.TownHalls)
                .MaxBy(townHall => _pathfinder.FindPath(townHall.Position.ToVector2(), _terrainTracker.EnemyStartingLocation).Count);

            Logger.Debug("Spawning 1 probe on the main");

            await _requestService.SendRequest(_requestBuilder.DebugCreateUnit(Owner.Enemy, Units.Probe, 3, main!.Position));
            _controller.SetRealTime("SpawnStuffScenario");

            _isScenarioDone = true;
        }
    }
}
