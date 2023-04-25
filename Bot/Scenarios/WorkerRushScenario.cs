using System.Threading.Tasks;
using Bot.GameData;
using Bot.MapKnowledge;
using Bot.Utils;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.Scenarios;

// Simulates a worker rush by smallBly https://aiarena.net/bots/338/
public class WorkerRushScenario: IScenario {
    private readonly IMapAnalyzer _mapAnalyzer;

    private const int DefaultTimingInSeconds = 50;

    private readonly int _timingInSeconds;
    private bool _isScenarioDone = false;

    public WorkerRushScenario(IMapAnalyzer mapAnalyzer, int timingInSeconds = DefaultTimingInSeconds) {
        _mapAnalyzer = mapAnalyzer;
        _timingInSeconds = timingInSeconds;
    }

    public async Task OnFrame() {
        if (_isScenarioDone) {
            return;
        }

        if (Controller.Frame >= TimeUtils.SecsToFrames(_timingInSeconds)) {
            var mainPosition = ExpandAnalyzer.Instance.GetExpand(Alliance.Self, ExpandType.Main).Position;

            Logger.Debug("Spawning 12 zerglings {0} units away from main", 0);

            // Spawned drones wouldn't be aggressive so we spawn zerglings instead
            await Program.GameConnection.SendRequest(RequestBuilder.DebugCreateUnit(Owner.Enemy, Units.Zergling, 12, _mapAnalyzer.WithWorldHeight(mainPosition)));
            Controller.SetRealTime("Worker rush started");

            _isScenarioDone = true;
        }
    }
}
