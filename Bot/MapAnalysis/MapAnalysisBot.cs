using System.Threading.Tasks;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.MapAnalysis;

public class MapAnalysisBot : IBot {
    private readonly IFrameClock _frameClock;

    public Race Race => Race.Zerg;

    public MapAnalysisBot(IFrameClock frameClock) {
        _frameClock = frameClock;
    }

    public Task OnFrame() {
        if (_frameClock.CurrentFrame == 0) {
            Logger.Important("Starting map analysis. Expect the game to freeze for a while.");
        }

        return Task.CompletedTask;
    }
}
