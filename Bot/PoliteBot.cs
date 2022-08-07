using System.Linq;
using Bot.GameData;
using Bot.GameSense;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot;

public abstract class PoliteBot: IBot {
    public abstract string Name { get; }

    public abstract Race Race { get; }

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
            Logger.Info("--------------------------------------");
        }

        if (Controller.Frame == Controller.SecsToFrames(1)) {
            Controller.Chat("Hi, my name is Sajuuk");
            Controller.Chat("I wish you good luck and good fun!");
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
