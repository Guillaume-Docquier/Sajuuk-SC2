using System.Threading.Tasks;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.Utils;
using Sajuuk.Wrapper;

namespace Sajuuk.Scenarios;

public class FlyingTerranScumScenario : IScenario {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IFrameClock _frameClock;
    private readonly IRequestBuilder _requestBuilder;
    private readonly ISc2Client _sc2Client;

    public FlyingTerranScumScenario(
        ITerrainTracker terrainTracker,
        IFrameClock frameClock,
        IRequestBuilder requestBuilder,
        ISc2Client sc2Client
    ) {
        _terrainTracker = terrainTracker;
        _frameClock = frameClock;
        _requestBuilder = requestBuilder;
        _sc2Client = sc2Client;
    }

    public async Task OnFrame() {
        if (_frameClock.CurrentFrame % TimeUtils.SecsToFrames(120) != 0) {
            return;
        }

        await _sc2Client.SendRequest(_requestBuilder.DebugCreateUnit(Owner.Enemy, Units.CommandCenterFlying, 1, _terrainTracker.WithWorldHeight(_terrainTracker.EnemyStartingLocation)));
    }
}
