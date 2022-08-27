using System;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot;

public class Program {
    private static readonly IBot Bot = new SajuukBot("1_10_10");

    private const string MapFileName = Maps.FileNames.Berlingrad;
    private const Race OpponentRace = Race.Terran;
    private const Difficulty OpponentDifficulty = Difficulty.Hard;

    private const bool RealTime = true;

    public static GameConnection GameConnection { get; private set; }
    public static bool DebugEnabled { get; private set; }

    public static void Main(string[] args) {
        try {
            if (args.Length == 0) {
                DebugEnabled = true;
                GameConnection = new GameConnection(runEvery: 2);
                GameConnection.RunSinglePlayer(Bot, MapFileName, OpponentRace, OpponentDifficulty, RealTime).Wait();
            }
            else {
                // On the ladder, for some reason, actions have a 1 frame delay before being received and applied
                // We will run every 2 frames, this way we won't notice the delay
                DebugEnabled = false;
                GameConnection = new GameConnection(runEvery: 2);
                GameConnection.RunLadder(Bot, args).Wait();
            }
        }
        catch (Exception ex) {
            Logger.Info(ex.ToString());
        }

        Logger.Info("Terminated.");
    }
}
