using System.Threading.Tasks;
using Bot.GameData;
using Bot.GameSense;
using Bot.Utils;
using Bot.Wrapper;

namespace Bot.Scenarios;

public class FlyingTerranScumScenario : IScenario {
    private readonly ITerrainTracker _terrainTracker;

    public FlyingTerranScumScenario(ITerrainTracker terrainTracker) {
        _terrainTracker = terrainTracker;
    }

    public async Task OnFrame() {
        if (Controller.Frame % TimeUtils.SecsToFrames(120) != 0) {
            return;
        }

        await Program.GameConnection.SendRequest(RequestBuilder.DebugCreateUnit(Owner.Enemy, Units.CommandCenterFlying, 1, _terrainTracker.WithWorldHeight(_terrainTracker.EnemyStartingLocation)));
    }
}
