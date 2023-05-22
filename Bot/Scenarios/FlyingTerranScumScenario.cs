using System.Threading.Tasks;
using Bot.GameData;
using Bot.GameSense;
using Bot.Requests;
using Bot.Utils;

namespace Bot.Scenarios;

public class FlyingTerranScumScenario : IScenario {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IFrameClock _frameClock;
    private readonly IRequestBuilder _requestBuilder;
    private readonly IRequestService _requestService;

    public FlyingTerranScumScenario(
        ITerrainTracker terrainTracker,
        IFrameClock frameClock,
        IRequestBuilder requestBuilder,
        IRequestService requestService
    ) {
        _terrainTracker = terrainTracker;
        _frameClock = frameClock;
        _requestBuilder = requestBuilder;
        _requestService = requestService;
    }

    public async Task OnFrame() {
        if (_frameClock.CurrentFrame % TimeUtils.SecsToFrames(120) != 0) {
            return;
        }

        await _requestService.SendRequest(_requestBuilder.DebugCreateUnit(Owner.Enemy, Units.CommandCenterFlying, 1, _terrainTracker.WithWorldHeight(_terrainTracker.EnemyStartingLocation)));
    }
}
