using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Bot.Debugging;
using Bot.Debugging.GraphicalDebugging;
using Bot.GameSense;
using Bot.GameSense.EnemyStrategyTracking;
using Bot.GameSense.RegionTracking;
using Bot.MapAnalysis.ExpandAnalysis;
using Bot.MapAnalysis.RegionAnalysis;
using Bot.MapAnalysis.RegionAnalysis.ChokePoints;
using Bot.Persistence;
using Bot.Scenarios;
using Bot.Tagging;
using Bot.VideoClips;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot;

[ExcludeFromCodeCoverage]
public class Program {
    private static readonly List<IScenario> Scenarios = new List<IScenario>
    {
        //new WorkerRushScenario(MapAnalyzer.Instance),
        //new FlyingTerranScumScenario(MapAnalyzer.Instance),
        //new SpawnStuffScenario(UnitsTracker.Instance, MapAnalyzer.Instance),
    };

    private const string Version = "4_0_4";
    private static readonly IBot Bot = CreateSajuuk(Version, Scenarios);

    public const string MapFileName = Maps.Season_2022_4.FileNames.Berlingrad;
    private const Race OpponentRace = Race.Random;
    private const Difficulty OpponentDifficulty = Difficulty.CheatInsane;

    private const bool RealTime = false;

    public static GameConnection GameConnection { get; private set; }
    public static bool DebugEnabled { get; private set; }

    public static IGraphicalDebugger GraphicalDebugger { get; set; }

    public static void Main(string[] args) {
        var regs = new List<Reg>
        {
            new Reg(1),
            new Reg(2),
            new Reg(3),
            new Reg(4),
            new Reg(5),
        };

        regs[0].Exp = new Exp(1, regs[0]);
        regs[1].Exp = new Exp(2, regs[1]);
        regs[2].Exp = new Exp(3, regs[2]);

        var repo = new JsonMapDataRepository<List<Reg>>("Data/Oops/Ahah/Lol/RegExp.json");
        repo.Save(regs);
        var loaded = repo.Load();

        var stop = 1;

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

    private static void EnableMapAnalysis() {
        ExpandAnalyzer.Instance.IsEnabled = true;
        RegionAnalyzer.Instance.IsEnabled = true;
    }

    private static void PlayDataGeneration() {
        Logger.Info("Game launched in data generation mode");
        foreach (var mapFileName in Maps.Season_2022_4.FileNames.GetAll()) {
            Logger.Info("Generating data for {0}", mapFileName);

            // TODO GD Instead of doing this, we should be able to use a different set of Controller, Bot and GameConnection
            EnableMapAnalysis();

            DebugEnabled = true;
            GraphicalDebugger = new NullGraphicalDebugger();

            GameConnection = CreateGameConnection();
            GameConnection.RunLocal(CreateSajuuk(Version, Scenarios), mapFileName, OpponentRace, OpponentDifficulty, realTime: false, runDataAnalyzersOnly: true).Wait();
        }
    }

    private static void PlayVideoClip() {
        Logger.Info("Game launched in video clip mode");

        DebugEnabled = true;
        // TODO GD GraphicalDebugger should be injected as well
        GraphicalDebugger = new Sc2GraphicalDebugger(TerrainTracker.Instance);

        GameConnection = CreateGameConnection(stepSize: 1);
        GameConnection.RunLocal(new VideoClipPlayer(MapFileName, DebuggingFlagsTracker.Instance, UnitsTracker.Instance, TerrainTracker.Instance), MapFileName, Race.Terran, Difficulty.VeryEasy, realTime: true).Wait();
    }

    private static void PlayLocalGame() {
        DebugEnabled = true;
        GraphicalDebugger = new Sc2GraphicalDebugger(TerrainTracker.Instance);

        GameConnection = CreateGameConnection();
        GameConnection.RunLocal(Bot, MapFileName, OpponentRace, OpponentDifficulty, RealTime).Wait();
    }

    private static void PlayLadderGame(string[] args) {
        DebugEnabled = false;
        GraphicalDebugger = new NullGraphicalDebugger();

        GameConnection = CreateGameConnection();
        GameConnection.RunLadder(Bot, args).Wait();
    }

    private static IBot CreateSajuuk(string version, List<IScenario> scenarios) {
        return new SajuukBot(
            version,
            scenarios,
            TaggingService.Instance,
            EnemyRaceTracker.Instance,
            VisibilityTracker.Instance,
            DebuggingFlagsTracker.Instance,
            UnitsTracker.Instance,
            IncomeTracker.Instance,
            TerrainTracker.Instance,
            BuildingTracker.Instance,
            RegionsTracker.Instance,
            CreepTracker.Instance,
            EnemyStrategyTracker.Instance,
            RegionsEvaluationsTracker.Instance
        );
    }

    private static GameConnection CreateGameConnection(uint stepSize = 2) {
        return new GameConnection(
            UnitsTracker.Instance,
            ExpandAnalyzer.Instance,
            RegionAnalyzer.Instance,
            stepSize
        );
    }

    private class Reg {
        [JsonInclude]
        public int Id { get; private set; }
        [JsonInclude]
        public Exp Exp { get; set; }

        [JsonConstructor]
        [Obsolete("Do not use this parameterless JsonConstructor", error: true)]
        public Reg() {}

        public Reg(int id) {
            Id = id;
        }
    }

    private class Exp {
        [JsonInclude]
        public int Id { get; private set; }
        [JsonInclude]
        public Reg Reg { get; private set; }

        [JsonConstructor]
        [Obsolete("Do not use this parameterless JsonConstructor", error: true)]
        public Exp() {}

        public Exp(int id, Reg reg) {
            Id = id;
            Reg = reg;
        }
    }
}
