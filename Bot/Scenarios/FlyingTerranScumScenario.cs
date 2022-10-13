using Bot.GameData;
using Bot.MapKnowledge;
using Bot.Wrapper;

namespace Bot.Scenarios;

public class FlyingTerranScumScenario : IScenario {
    public void OnFrame() {
        if (Controller.Frame % Controller.SecsToFrames(120) != 0) {
            return;
        }

#pragma warning disable CS4014
        Program.GameConnection.SendRequest(RequestBuilder.DebugCreateUnit(Owner.Enemy, Units.CommandCenterFlying, 1, MapAnalyzer.EnemyStartingLocation));
#pragma warning restore CS4014
    }
}
