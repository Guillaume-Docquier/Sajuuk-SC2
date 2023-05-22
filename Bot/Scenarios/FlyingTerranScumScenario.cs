using System.Threading.Tasks;
using Bot.GameData;
using Bot.GameSense;
using Bot.Utils;
using Bot.Wrapper;

namespace Bot.Scenarios;

public class FlyingTerranScumScenario : IScenario {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IFrameClock _frameClock;
    private readonly IRequestBuilder _requestBuilder;

    public FlyingTerranScumScenario(
        ITerrainTracker terrainTracker,
        IFrameClock frameClock,
        IRequestBuilder requestBuilder
    ) {
        _terrainTracker = terrainTracker;
        _frameClock = frameClock;
        _requestBuilder = requestBuilder;
    }

    public async Task OnFrame() {
        if (_frameClock.CurrentFrame % TimeUtils.SecsToFrames(120) != 0) {
            return;
        }

        await Program.GameConnection.SendRequest(_requestBuilder.DebugCreateUnit(Owner.Enemy, Units.CommandCenterFlying, 1, _terrainTracker.WithWorldHeight(_terrainTracker.EnemyStartingLocation)));
    }
}
