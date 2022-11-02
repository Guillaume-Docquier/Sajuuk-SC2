﻿using System;
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

    private static readonly IBot Bot = new SajuukBot("3_0_0", scenarios: Scenarios);

    private const string MapFileName = Maps.Season_2022_4.FileNames.Stargazers;
    private const Race OpponentRace = Race.Zerg;
    private const Difficulty OpponentDifficulty = Difficulty.Hard;

    private const bool RealTime = true;

    public static GameConnection GameConnection { get; private set; }
    public static bool DebugEnabled { get; private set; }

    public static IGraphicalDebugger GraphicalDebugger { get; set; }

    public static void Main(string[] args) {
        try {
            // Data generation
            if (args.Length == 1 && args[0] == "--generateData") {
                Logger.Info("Game launched in data generation mode");
                foreach (var mapFileName in Maps.Season_2022_4.FileNames.GetAll()) {
                    Logger.Info("Generating data for {0}", mapFileName);

                    DisableDataStores();

                    DebugEnabled = true;
                    GraphicalDebugger = new LadderGraphicalDebugger();

                    GameConnection = new GameConnection(stepSize: 2);
                    GameConnection.RunLocal(new SajuukBot("3_0_0", scenarios: Scenarios), mapFileName, OpponentRace, OpponentDifficulty, realTime: false, runDataAnalyzersOnly: true).Wait();
                }
            }
            // Video clip
            if (args.Length == 1 && args[0] == "--videoClip") {
                Logger.Info("Game launched in video clip mode");

                DebugEnabled = true;
                GraphicalDebugger = new LocalGraphicalDebugger();

                GameConnection = new GameConnection(stepSize: 1);
                GameConnection.RunLocal(new VideoClipPlayer(), MapFileName, OpponentRace, Difficulty.VeryEasy, realTime: true).Wait();
            }
            // Local
            else if (args.Length == 0) {
                DebugEnabled = true;
                GraphicalDebugger = new LocalGraphicalDebugger();

                GameConnection = new GameConnection(stepSize: 2);
                GameConnection.RunLocal(Bot, MapFileName, OpponentRace, OpponentDifficulty, RealTime).Wait();
            }
            // Ladder
            else {
                DebugEnabled = false;
                GraphicalDebugger = new LadderGraphicalDebugger();

                // On the ladder, for some reason, actions have a 1 frame delay before being received and applied
                // We will run every 2 frames, this way we won't notice the delay
                GameConnection = new GameConnection(stepSize: 2);
                GameConnection.RunLadder(Bot, args).Wait();
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
}
