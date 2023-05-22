using System.Threading.Tasks;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.MapAnalysis;

public class MapAnalysisRunner : IBot {
    private readonly IFrameClock _frameClock;

    public string Name => "MapAnalysisRunner";
    public Race Race => Race.Zerg;

    public MapAnalysisRunner(IFrameClock frameClock) {
        _frameClock = frameClock;
    }

    public Task OnFrame() {
        if (_frameClock.CurrentFrame == 0) {
            Logger.Important("Starting map analysis. Expect the game to freeze for a while.");
        }

        return Task.CompletedTask;
    }
}
