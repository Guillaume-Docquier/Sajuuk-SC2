using System.Linq;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapKnowledge;
using Bot.Wrapper;

namespace Bot.Scenarios;

public class SpawnStuffScenario : IScenario {
    private bool _isScenarioDone = false;

    public void OnFrame() {
        if (_isScenarioDone) {
            return;
        }

        if (Controller.Frame >= Controller.SecsToFrames(1)) {
            var main = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.TownHalls)
                .MaxBy(townHall => Pathfinder.FindPath(townHall.Position, MapAnalyzer.EnemyStartingLocation).Count);

            Logger.Debug("Spawning 1 observer on the main");

            // We don't await, not ideal but we don't need to
            // Making all the code async just for us would be a chore
#pragma warning disable CS4014
            Program.GameConnection.SendRequest(RequestBuilder.DebugCreateUnit(Owner.Enemy, Units.Ravager, 10, main!.Position));
#pragma warning restore CS4014

            _isScenarioDone = true;
        }
    }
}
