using System;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot;

internal class Program {
    private static readonly IBot Bot = new ZergBot();
    private const string MapName = "AcropolisLE.SC2Map";

    private const Race OpponentRace = Race.Random;
    private const Difficulty OpponentDifficulty = Difficulty.Medium;

    private const bool RealTime = false;

    public static GameConnection GameConnection;

    private static void Main(string[] args) {
        try {
            GameConnection = new GameConnection();
            if (args.Length == 0) {
                GameConnection.RunSinglePlayer(Bot, MapName, OpponentRace, OpponentDifficulty, RealTime).Wait();
            }
            else {
                GameConnection.RunLadder(Bot, args).Wait();
            }
        }
        catch (Exception ex) {
            Logger.Info(ex.ToString());
        }

        Logger.Info("Terminated.");
    }
}
