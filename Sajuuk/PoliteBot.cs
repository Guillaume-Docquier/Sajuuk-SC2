using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.Scenarios;
using Sajuuk.Tagging;
using Sajuuk.Utils;
using Sajuuk.Wrapper;
using SC2APIProtocol;

namespace Sajuuk;

public abstract class PoliteBot : IBot {
    private readonly ITaggingService _taggingService;
    protected readonly IUnitsTracker UnitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IFrameClock _frameClock;
    private readonly IController _controller;
    private readonly IChatService _chatService;

    private readonly string _version;
    private readonly List<IScenario> _scenarios;
    private bool _greetDone = false;
    private bool _admittedDefeat = false;

    public abstract string Name { get; }

    public abstract Race Race { get; }

    protected PoliteBot(
        string version,
        List<IScenario> scenarios,
        ITaggingService taggingService,
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IFrameClock frameClock,
        IController controller,
        IChatService chatService
    ) {
        _version = version;
        _scenarios = scenarios;

        _taggingService = taggingService;
        UnitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _frameClock = frameClock;
        _controller = controller;
        _chatService = chatService;
    }

    public async Task OnFrame() {
        EnsureGreeting();
        EnsureGg();

        await PlayScenarios();
        await DoOnFrame();
    }

    private void EnsureGreeting() {
        if (_frameClock.CurrentFrame == 0) {
            Logger.Info("--------------------------------------");
            Logger.Metric("Bot: {0}", Name);
            Logger.Metric("Map: {0}", _controller.GameInfo.MapName);
            Logger.Metric("Starting Corner: {0}", _terrainTracker.GetStartingCorner());
            Logger.Metric("Bot Version: {0}", _version);
            Logger.Info("--------------------------------------");
        }

        if (!_greetDone && _frameClock.CurrentFrame >= TimeUtils.SecsToFrames(1)) {
            _chatService.Chat($"Hi, my name is {Name}");
            _chatService.Chat("I wish you good luck and good fun!");
            _taggingService.TagVersion(_version);
            _greetDone = true;
        }
    }

    private void EnsureGg() {
        var structures = UnitsTracker.GetUnits(UnitsTracker.OwnedUnits, Units.Buildings).ToList();
        if (!_admittedDefeat && structures.Count == 1 && structures.First().Integrity < 0.4) {
            _chatService.Chat("gg wp");
            _admittedDefeat = true;
        }
    }

    private async Task PlayScenarios() {
        if (!Program.DebugEnabled) {
            return;
        }

        foreach (var scenario in _scenarios) {
            await scenario.OnFrame();
        }
    }

    protected abstract Task DoOnFrame();
}
