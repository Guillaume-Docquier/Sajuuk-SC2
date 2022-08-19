using System.Linq;
using Bot.GameData;
using Bot.GameSense;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot;

public abstract class PoliteBot: IBot {
    private readonly string _version;
    private bool _greetDone = false;

    public abstract string Name { get; }

    public abstract Race Race { get; }

    public PoliteBot(string version) {
        _version = version;
    }

    public void OnFrame() {
        EnsureGreeting();
        EnsureGG();

        DoOnFrame();
    }

    private void EnsureGreeting() {
        if (Controller.Frame == 0) {
            Logger.Info(Name);
            Logger.Info("--------------------------------------");
            Logger.Info("Map: {0}", Controller.GameInfo.MapName);
            Logger.Info("Version: {0}", _version);
            // TODO GD Add the starting corner
            Logger.Info("--------------------------------------");
        }

        if (!_greetDone && Controller.Frame >= Controller.SecsToFrames(1)) {
            Controller.Chat("Hi, my name is Sajuuk");
            Controller.Chat("I wish you good luck and good fun!");
            Controller.Chat($"Tag:v{_version}");

            _greetDone = true;
        }
    }

    private void EnsureGG() {
        var structures = Controller.GetUnits(UnitsTracker.OwnedUnits, Units.Structures).ToList();
        if (structures.Count == 1 && structures.First().Integrity < 0.4) {
            if (!Controller.ChatLog.Contains("gg wp")) {
                // Controller.Chat("gg wp");
            }
        }
    }

    protected abstract void DoOnFrame();
}
