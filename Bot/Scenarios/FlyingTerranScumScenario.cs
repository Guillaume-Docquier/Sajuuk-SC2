using System.Threading.Tasks;
using Bot.GameData;
using Bot.MapKnowledge;
using Bot.Utils;
using Bot.Wrapper;

namespace Bot.Scenarios;

public class FlyingTerranScumScenario : IScenario {
    private readonly IMapAnalyzer _mapAnalyzer;

    public FlyingTerranScumScenario(IMapAnalyzer mapAnalyzer) {
        _mapAnalyzer = mapAnalyzer;
    }

    public async Task OnFrame() {
        if (Controller.Frame % TimeUtils.SecsToFrames(120) != 0) {
            return;
        }

        await Program.GameConnection.SendRequest(RequestBuilder.DebugCreateUnit(Owner.Enemy, Units.CommandCenterFlying, 1, _mapAnalyzer.WithWorldHeight(_mapAnalyzer.EnemyStartingLocation)));
    }
}
