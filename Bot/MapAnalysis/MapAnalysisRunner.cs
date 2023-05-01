using System;
using System.Threading.Tasks;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.MapAnalysis;

public class MapAnalysisRunner : IBot {
    private readonly Func<uint> _getCurrentFrame;

    public string Name => "MapAnalysisRunner";
    public Race Race => Race.Zerg;

    public MapAnalysisRunner(Func<uint> getCurrentFrame) {
        _getCurrentFrame = getCurrentFrame;
    }

    public Task OnFrame() {
        if (_getCurrentFrame() == 0) {
            Logger.Important("Starting map analysis. Expect the game to freeze for a while.");
        }

        return Task.CompletedTask;
    }
}
