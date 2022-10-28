using System.Collections.Generic;
using System.Linq;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapKnowledge;
using Bot.Scenarios;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot;

public abstract class PoliteBot: IBot {
    private readonly string _version;
    private readonly List<IScenario> _scenarios;
    private bool _greetDone = false;
    private bool _admittedDefeat = false;

    public abstract string Name { get; }

    public abstract Race Race { get; }

    public PoliteBot(string version, List<IScenario> scenarios = null) {
        _version = version;
        _scenarios = scenarios;
    }

    public void OnFrame() {
        //EnsureGreeting();
        EnsureGG();

        //PlayScenarios();
        DoOnFrame();
    }

    private void EnsureGreeting() {
        if (Controller.Frame == 0) {
            Logger.Info("--------------------------------------");
            Logger.Metric("Bot: {0}", Name);
            Logger.Metric("Map: {0}", Controller.GameInfo.MapName);
            Logger.Metric("Starting Corner: {0}", MapAnalyzer.GetStartingCorner());
            Logger.Metric("Bot Version: {0}", _version);
            Logger.Info("--------------------------------------");
        }

        if (!_greetDone && Controller.Frame >= Controller.SecsToFrames(1)) {
            Controller.Chat($"Hi, my name is {Name}");
            Controller.Chat("I wish you good luck and good fun!");
            Controller.TagGame($"v{_version}");

            _greetDone = true;
        }
    }

    private void EnsureGG() {
        var structures = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Buildings).ToList();
        if (!_admittedDefeat && structures.Count == 1 && structures.First().Integrity < 0.4) {
            Controller.Chat("gg wp");
            _admittedDefeat = true;
        }
    }

    private void PlayScenarios() {
        if (Program.DebugEnabled) {
            _scenarios?.ForEach(scenario => scenario.OnFrame());
        }
    }

    protected abstract void DoOnFrame();
}
