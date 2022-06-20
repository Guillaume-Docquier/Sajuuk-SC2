using System;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot {
    internal class Program {
        // Settings for your bot.
        private static readonly IBot Bot = new RaxBot();
        private const Race MyRace = Race.Terran;
        private const string MapName = "AcropolisLE.SC2Map";

        private const Race OpponentRace = Race.Random;
        private const Difficulty OpponentDifficulty = Difficulty.Easy;

        private const bool RealTime = true;

        public static GameConnection GameConnection;

        private static void Main(string[] args) {
            try {
                GameConnection = new GameConnection();
                if (args.Length == 0) {
                    GameConnection.FindExecutablePath();
                    GameConnection.RunSinglePlayer(Bot, MapName, MyRace, OpponentRace, OpponentDifficulty, RealTime).Wait();
                }
                else {
                    GameConnection.RunLadder(Bot, MyRace, args).Wait();
                }
            }
            catch (Exception ex) {
                Logger.Info(ex.ToString());
            }

            Logger.Info("Terminated.");
        }
    }
}
