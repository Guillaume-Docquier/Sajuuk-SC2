using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bot.GameData;
using Bot.GameSense;
using Bot.Scenarios;
using Bot.Tagging;
using Bot.Utils;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot;

public abstract class PoliteBot: IBot {
    private readonly ITaggingService _taggingService;
    protected readonly IUnitsTracker UnitsTracker;
    private readonly ITerrainTracker _terrainTracker;

    private readonly string _version;
    private readonly List<IScenario> _scenarios;
    private bool _greetDone = false;
    private bool _admittedDefeat = false;

    public abstract string Name { get; }

    public abstract Race Race { get; }

    protected PoliteBot(string version, List<IScenario> scenarios, ITaggingService taggingService, IUnitsTracker unitsTracker, ITerrainTracker terrainTracker) {
        _version = version;
        _scenarios = scenarios;

        _taggingService = taggingService;
        UnitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
    }

    public async Task OnFrame() {
        EnsureGreeting();
        EnsureGg();

        await PlayScenarios();
        await DoOnFrame();
    }

    private void EnsureGreeting() {
        if (Controller.Frame == 0) {
            Logger.Info("--------------------------------------");
            Logger.Metric("Bot: {0}", Name);
            Logger.Metric("Map: {0}", Controller.GameInfo.MapName);
            Logger.Metric("Starting Corner: {0}", _terrainTracker.GetStartingCorner());
            Logger.Metric("Bot Version: {0}", _version);
            Logger.Info("--------------------------------------");
        }

        if (!_greetDone && Controller.Frame >= TimeUtils.SecsToFrames(1)) {
            Controller.Chat($"Hi, my name is {Name}");
            Controller.Chat("I wish you good luck and good fun!");
            _taggingService.TagVersion(_version);
            _greetDone = true;
        }
    }

    private void EnsureGg() {
        var structures = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Buildings).ToList();
        if (!_admittedDefeat && structures.Count == 1 && structures.First().Integrity < 0.4) {
            Controller.Chat("gg wp");
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
