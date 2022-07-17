using System;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot;

public class Program {
    private static readonly IBot Bot = new SajuukBot();

    private const string MapName = "BlackburnAIE.SC2Map";
    private const Race OpponentRace = Race.Random;
    private const Difficulty OpponentDifficulty = Difficulty.Easy;

    private const bool RealTime = true;

    public static GameConnection GameConnection;

    public static void Main(string[] args) {
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
