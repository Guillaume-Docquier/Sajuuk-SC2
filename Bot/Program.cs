using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Bot.MapKnowledge;
using Bot.Scenarios;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot;

[ExcludeFromCodeCoverage]
public class Program {
    private static readonly List<IScenario> Scenarios = new List<IScenario>
    {
        //new WorkerRushScenario(),
    };

    private static readonly IBot Bot = new SajuukBot("2_5_2", scenarios: Scenarios);

    private const string MapFileName = Maps.Season_2022_4.FileNames.InsideAndOut;
    private const Race OpponentRace = Race.Terran;
    private const Difficulty OpponentDifficulty = Difficulty.Hard;

    private const bool RealTime = true;

    public static GameConnection GameConnection { get; private set; }
    public static bool DebugEnabled { get; private set; }

    public static IGraphicalDebugger GraphicalDebugger { get; set; }

    public static void Main(string[] args) {
        try {
            if (args.Length == 1 && args[0] == "--generateData") {
                Logger.Info("Game launched in data generation mode");
                foreach (var mapFileName in Maps.Season_2022_4.FileNames.GetAll()) {
                    Logger.Info("Generating data for {0}", mapFileName);
                    ExpandDataStore.IsEnabled = false;
                    RegionDataStore.IsEnabled = false;

                    DebugEnabled = true;
                    GraphicalDebugger = new LadderGraphicalDebugger();

                    GameConnection = new GameConnection(runEvery: 2);
                    GameConnection.RunLocal(new SajuukBot("2_3_0", scenarios: Scenarios), mapFileName, OpponentRace, OpponentDifficulty, realTime: false, runDataAnalyzersOnly: true).Wait();
                }
            }
            else if (args.Length == 0) {
                DebugEnabled = true;
                GraphicalDebugger = new LocalGraphicalDebugger();

                GameConnection = new GameConnection(runEvery: 2);
                GameConnection.RunLocal(Bot, MapFileName, OpponentRace, OpponentDifficulty, RealTime).Wait();
            }
            else {
                DebugEnabled = false;
                GraphicalDebugger = new LadderGraphicalDebugger();

                // On the ladder, for some reason, actions have a 1 frame delay before being received and applied
                // We will run every 2 frames, this way we won't notice the delay
                GameConnection = new GameConnection(runEvery: 2);
                GameConnection.RunLadder(Bot, args).Wait();
            }
        }
        catch (Exception ex) {
            Logger.Error(ex.ToString());
        }

        Logger.Info("Terminated.");
    }
}
