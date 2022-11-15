using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Bot.Debugging.GraphicalDebugging;
using Bot.MapKnowledge;
using Bot.Scenarios;
using Bot.VideoClips;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot;

[ExcludeFromCodeCoverage]
public class Program {
    private static readonly List<IScenario> Scenarios = new List<IScenario>
    {
        //new WorkerRushScenario(),
    };

    private const string Version = "3_0_0";
    private static readonly IBot Bot = new SajuukBot(Version, scenarios: Scenarios);

    private const string MapFileName = Maps.Season_2022_4.FileNames.Moondance;
    private const Race OpponentRace = Race.Zerg;
    private const Difficulty OpponentDifficulty = Difficulty.Hard;

    private const bool RealTime = true;

    public static GameConnection GameConnection { get; private set; }
    public static bool DebugEnabled { get; private set; }

    public static IGraphicalDebugger GraphicalDebugger { get; set; }

    public static void Main(string[] args) {
        try {
            switch (args.Length) {
                case 1 when args[0] == "--generateData":
                    PlayDataGeneration();
                    break;
                case 1 when args[0] == "--videoClip":
                    PlayVideoClip();
                    break;
                case 0:
                    PlayLocalGame();
                    break;
                default:
                    PlayLadderGame(args);
                    break;
            }
        }
        catch (Exception ex) {
            Logger.Error(ex.ToString());
        }

        Logger.Info("Terminated.");
    }

    private static void DisableDataStores() {
        ExpandLocationDataStore.IsEnabled = false;
        RegionDataStore.IsEnabled = false;
        RayCastingChokeFinder.VisionLinesDataStore.IsEnabled = false;
    }

    private static void PlayDataGeneration() {
        Logger.Info("Game launched in data generation mode");
        foreach (var mapFileName in Maps.Season_2022_4.FileNames.GetAll()) {
            Logger.Info("Generating data for {0}", mapFileName);

            DisableDataStores();

            DebugEnabled = true;
            GraphicalDebugger = new NullGraphicalDebugger();

            GameConnection = new GameConnection();
            GameConnection.RunLocal(new SajuukBot(Version, scenarios: Scenarios), mapFileName, OpponentRace, OpponentDifficulty, realTime: false, runDataAnalyzersOnly: true).Wait();
        }
    }

    private static void PlayVideoClip() {
        Logger.Info("Game launched in video clip mode");

        DebugEnabled = true;
        GraphicalDebugger = new Sc2GraphicalDebugger();

        GameConnection = new GameConnection(stepSize: 1);
        GameConnection.RunLocal(new VideoClipPlayer(MapFileName), MapFileName, Race.Terran, Difficulty.VeryEasy, realTime: true).Wait();
    }

    private static void PlayLocalGame() {
        DebugEnabled = true;
        GraphicalDebugger = new Sc2GraphicalDebugger();

        GameConnection = new GameConnection();
        GameConnection.RunLocal(Bot, MapFileName, OpponentRace, OpponentDifficulty, RealTime).Wait();
    }

    private static void PlayLadderGame(string[] args) {
        DebugEnabled = false;
        GraphicalDebugger = new NullGraphicalDebugger();

        GameConnection = new GameConnection();
        GameConnection.RunLadder(Bot, args).Wait();
    }
}
