using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapKnowledge;
using Bot.Wrapper;

namespace Bot.Scenarios;

// Simulates a worker rush by smallBly https://aiarena.net/bots/338/
public class WorkerRushScenario: IScenario {
    private const int DefaultTimingInSeconds = 1 * 60 + 50;
    private const int SpawnDistance = 4; // Must be in aggro range otherwise they run away

    private readonly int _timingInSeconds;
    private bool _isScenarioDone = false;

    public WorkerRushScenario(int timingInSeconds = DefaultTimingInSeconds) {
        _timingInSeconds = timingInSeconds;
    }

    public void OnFrame() {
        if (_isScenarioDone) {
            return;
        }

        if (Controller.Frame >= Controller.SecsToFrames(_timingInSeconds)) {
            var natural = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.TownHalls)
                .MinBy(townHall => Pathfinder.FindPath(townHall.Position, MapAnalyzer.EnemyStartingLocation).Count);

            var pathFromNatural = Pathfinder.FindPath(natural!.Position, MapAnalyzer.EnemyStartingLocation);

            Logger.Debug("Spawning 12 zerglings {0} units away from natural", pathFromNatural[SpawnDistance].HorizontalDistanceTo(natural));

            // We don't await, not ideal but we don't need to
            // Making all the code async just for us would be a chore
#pragma warning disable CS4014
            Program.GameConnection.SendRequest(RequestBuilder.DebugCreateUnit(Owner.Enemy, Units.Zergling, 12, pathFromNatural[SpawnDistance]));
#pragma warning restore CS4014

            _isScenarioDone = true;
        }
    }
}
