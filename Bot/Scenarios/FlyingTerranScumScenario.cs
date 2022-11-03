using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.MapKnowledge;
using Bot.Utils;
using Bot.Wrapper;

namespace Bot.Scenarios;

public class FlyingTerranScumScenario : IScenario {
    public void OnFrame() {
        if (Controller.Frame % TimeUtils.SecsToFrames(120) != 0) {
            return;
        }

#pragma warning disable CS4014
        Program.GameConnection.SendRequest(RequestBuilder.DebugCreateUnit(Owner.Enemy, Units.CommandCenterFlying, 1, MapAnalyzer.EnemyStartingLocation.ToVector3()));
#pragma warning restore CS4014
    }
}
