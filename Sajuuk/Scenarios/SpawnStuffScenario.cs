using System.Linq;
using System.Threading.Tasks;
using Sajuuk.ExtensionMethods;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.MapAnalysis;
using Sajuuk.Utils;
using Sajuuk.Wrapper;

namespace Sajuuk.Scenarios;

public class SpawnStuffScenario : IScenario {
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IFrameClock _frameClock;
    private readonly IController _controller;
    private readonly IPathfinder _pathfinder;
    private readonly IRequestBuilder _requestBuilder;
    private readonly ISc2Client _sc2Client;

    private bool _isScenarioDone = false;

    public SpawnStuffScenario(
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IFrameClock frameClock,
        IController controller,
        IPathfinder pathfinder,
        IRequestBuilder requestBuilder,
        ISc2Client sc2Client
    ) {
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _frameClock = frameClock;
        _controller = controller;
        _pathfinder = pathfinder;
        _requestBuilder = requestBuilder;
        _sc2Client = sc2Client;
    }

    public async Task OnFrame() {
        if (_isScenarioDone) {
            return;
        }

        if (_frameClock.CurrentFrame >= TimeUtils.SecsToFrames(50)) {
            var main = _unitsTracker.GetUnits(_unitsTracker.OwnedUnits, Units.TownHalls)
                .MaxBy(townHall => _pathfinder.FindPath(townHall.Position.ToVector2(), _terrainTracker.EnemyStartingLocation).Count);

            Logger.Debug("Spawning 1 probe on the main");

            await _sc2Client.SendRequest(_requestBuilder.DebugCreateUnit(UnitOwner.Enemy, Units.Probe, 3, main!.Position));
            _controller.SetRealTime("SpawnStuffScenario");

            _isScenarioDone = true;
        }
    }
}
