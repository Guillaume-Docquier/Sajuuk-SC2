using System;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot;

public class Program {
    private static readonly IBot Bot = new SajuukBot("1_10_2");

    public const string MapName = Maps.FileNames.TwoThousandAtmospheres;
    private const Race OpponentRace = Race.Terran;
    private const Difficulty OpponentDifficulty = Difficulty.Hard;

    private const bool RealTime = false;

    public static GameConnection GameConnection;

    public static void Main(string[] args) {
        try {
            if (args.Length == 0) {
                GraphicalDebugger.IsActive = true;
                GameConnection = new GameConnection(runEvery: 2);
                GameConnection.RunSinglePlayer(Bot, MapName, OpponentRace, OpponentDifficulty, RealTime).Wait();
            }
            else {
                // On the ladder, for some reason, actions have a 1 frame delay before being received and applied
                // We will run every 2 frames, this way we won't notice the delay
                GraphicalDebugger.IsActive = false;
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
