using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapKnowledge;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.Wrapper;

public class GameConnection {
    private const string Address = "127.0.0.1";
    private readonly ProtobufProxy _proxy = new ProtobufProxy();

    private string _starcraftExe;
    private string _starcraftDir;
    private string _starcraftMapsDir;

    private readonly uint _stepSize;
    private static readonly ulong DebugMemoryEvery = TimeUtils.SecsToFrames(5);

    private readonly PerformanceDebugger _performanceDebugger = new PerformanceDebugger();

    // On the ladder, for some reason, actions have a 1 frame delay before being received and applied
    // We will run every 2 frames by default, this way we won't notice the delay
    // Lower than 2 is not recommended unless your code is crazy good and can handle the inevitable desync
    public GameConnection(uint stepSize = 20) {
        _stepSize = stepSize;
    }

    private void FindExecutableInfo() {
        var myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var executeInfo = Path.Combine(myDocuments, "StarCraft II", "ExecuteInfo.txt");

        if (File.Exists(executeInfo)) {
            var lines = File.ReadAllLines(executeInfo);
            foreach (var line in lines) {
                if (line.Trim().StartsWith("executable")) {
                    _starcraftExe = line.Substring(line.IndexOf('=') + 1).Trim();
                    _starcraftDir = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(_starcraftExe))); //we need 2 folders down
                    if (_starcraftDir != null) {
                        _starcraftMapsDir = Path.Combine(_starcraftDir, "Maps");
                    }

                    break;
                }
            }
        }

        if (_starcraftExe == default) {
            throw new Exception($"Unable to find:{executeInfo}. Make sure you started the game successfully at least once.");
        }
    }

    public async Task RunLocal(IBot bot, string mapFileName, Race opponentRace, Difficulty opponentDifficulty, bool realTime, bool runDataAnalyzersOnly = false) {
        const int port = 5678;

        Logger.Info("Finding the SC2 executable info");
        FindExecutableInfo();

        Logger.Info("Starting SinglePlayer Instance");
        StartSc2Instance(port);

        Logger.Info("Connecting to port: {0}", port);
        await Connect(port);

        Logger.Info("Creating game on map: {0}", mapFileName);
        await CreateGame(mapFileName, opponentRace, opponentDifficulty, realTime);

        Logger.Info("Joining game");
        var playerId = await JoinGame(bot.Race);
        await Run(bot, playerId, runDataAnalyzersOnly);
    }

    private void StartSc2Instance(int port) {
        var processStartInfo = new ProcessStartInfo(_starcraftExe)
        {
            // TODO GD Make and enum for this
            // DisplayMode 0: Windowed
            // DisplayMode 1: Full screen
            Arguments = $"-listen {Address} -port {port} -displayMode 1",
            WorkingDirectory = Path.Combine(_starcraftDir, "Support64")
        };

        Logger.Info("Launching SC2:");
        Logger.Info("--> File: {0}", _starcraftExe);
        Logger.Info("--> Working Dir: {0}", processStartInfo.WorkingDirectory);
        Logger.Info("--> Arguments: {0}", processStartInfo.Arguments);
        Process.Start(processStartInfo);
    }

    private async Task Connect(int port) {
        const int timeout = 60;
        for (var i = 0; i < timeout * 2; i++) {
            try {
                await _proxy.Connect(Address, port);
                Logger.Info("--> Connected");

                return;
            }
            catch (WebSocketException) {
                Logger.Warning("Failed. Retrying...");
            }

            Thread.Sleep(500);
        }

        Logger.Error("Unable to connect to SC2 after {0} seconds.", timeout);
        throw new Exception("Unable to make a connection.");
    }

    private async Task CreateGame(string mapFileName, Race opponentRace, Difficulty opponentDifficulty, bool realTime) {;
        var mapPath = Path.Combine(_starcraftMapsDir, mapFileName);
        if (!File.Exists(mapPath)) {
            Logger.Error($"Unable to locate map: {mapPath}");
            throw new Exception($"Unable to locate map: {mapPath}");
        }

        var createGameResponse = await SendRequest(RequestBuilder.RequestCreateComputerGame(realTime, mapPath, opponentRace, opponentDifficulty), logErrors: true);

        // TODO GD This might be broken now, used to be ResponseJoinGame.Types.Error.Unset (0) but it doesn't exist anymore
        if (createGameResponse.CreateGame.Error != ResponseCreateGame.Types.Error.MissingMap) {
            Logger.Error("CreateGame error: {0}", createGameResponse.CreateGame.Error.ToString());
            if (!string.IsNullOrEmpty(createGameResponse.CreateGame.ErrorDetails)) {
                Logger.Error(createGameResponse.CreateGame.ErrorDetails);
            }
        }
    }

    private async Task<uint> JoinGame(Race race) {
        var joinGameResponse = await SendRequest(RequestBuilder.RequestJoinLocalGame(race), logErrors: true);

        // TODO GD This might be broken now, used to be ResponseJoinGame.Types.Error.Unset (0) but it doesn't exist anymore
        if (joinGameResponse.JoinGame.Error != ResponseJoinGame.Types.Error.MissingParticipation) {
            Logger.Error("JoinGame error: {0}", joinGameResponse.JoinGame.Error.ToString());
            if (!string.IsNullOrEmpty(joinGameResponse.JoinGame.ErrorDetails)) {
                Logger.Error(joinGameResponse.JoinGame.ErrorDetails);
            }
        }

        return joinGameResponse.JoinGame.PlayerId;
    }

    public async Task Ping() {
        await _proxy.Ping();
    }

    public async Task RunLadder(IBot bot, string[] args) {
        var commandLineArgs = new CommandLineArguments(args);
        await RunLadder(bot, commandLineArgs.GamePort, commandLineArgs.StartPort);
    }

    private async Task RunLadder(IBot bot, int gamePort, int startPort) {
        await Connect(gamePort);

        var playerId = await JoinGameLadder(bot.Race, startPort);
        await Run(bot, playerId);
    }

    private async Task<uint> JoinGameLadder(Race race, int startPort) {
        var joinGameResponse = await SendRequest(RequestBuilder.RequestJoinLadderGame(race, startPort), logErrors: true);

        // TODO GD This might be broken now, used to be ResponseJoinGame.Types.Error.Unset (0) but it doesn't exist anymore
        if (joinGameResponse.JoinGame.Error != ResponseJoinGame.Types.Error.MissingParticipation) {
            Logger.Error("JoinGame error: {0}", joinGameResponse.JoinGame.Error.ToString());
            if (!string.IsNullOrEmpty(joinGameResponse.JoinGame.ErrorDetails)) {
                Logger.Error(joinGameResponse.JoinGame.ErrorDetails);
            }
        }

        return joinGameResponse.JoinGame.PlayerId;
    }

    private async Task Run(IBot bot, uint playerId, bool runDataAnalyzersOnly = false) {
        // We use this to generate data for all maps by running a single frame on each of them
        // We need to reset because everything is global static
        if (runDataAnalyzersOnly) {
            Controller.Reset();
        }

        var dataRequest = new Request
        {
            Data = new RequestData
            {
                UnitTypeId = true,
                AbilityId = true,
                BuffId = true,
                EffectId = true,
                UpgradeId = true,
            }
        };
        var dataResponse = await SendRequest(dataRequest);
        KnowledgeBase.Data = dataResponse.Data;

        while (true) {
            // Controller.Frame is uint.MaxValue until we request frame 0
            var nextFrame = Controller.Frame == uint.MaxValue ? 0 : Controller.Frame + _stepSize;
            var observationResponse = await SendRequest(RequestBuilder.RequestObservation(nextFrame));

            if (observationResponse.Status is Status.Quit) {
                Logger.Info("Game was terminated.");
                break;
            }

            var observation = observationResponse.Observation;
            if (observationResponse.Status is Status.Ended) {
                _performanceDebugger.LogAveragePerformance();

                foreach (var result in observation.PlayerResult) {
                    if (result.PlayerId == playerId) {
                        Logger.Info("Result: {0}", result.Result);
                    }
                }

                break;
            }

            await RunBot(bot, observation);

            if (observation.Observation.GameLoop % DebugMemoryEvery == 0) {
                PrintMemoryInfo();
            }

            if (runDataAnalyzersOnly && ExpandAnalyzer.IsInitialized && RegionAnalyzer.IsInitialized) {
                await Quit();
            }
            else {
                await SendRequest(RequestBuilder.RequestStep(_stepSize));
            }
        }
    }

    private async Task RunBot(IBot bot, ResponseObservation observation) {
        var gameInfoResponse = await SendRequest(RequestBuilder.RequestGameInfo());

        _performanceDebugger.FrameStopwatch.Start();

        _performanceDebugger.ControllerStopwatch.Start();
        Controller.NewFrame(gameInfoResponse.GameInfo, observation);
        _performanceDebugger.ControllerStopwatch.Stop();

        _performanceDebugger.BotStopwatch.Start();
        await bot.OnFrame();
        _performanceDebugger.BotStopwatch.Stop();

        _performanceDebugger.ActionsStopwatch.Start();
        var actions = Controller.GetActions().ToList();

        if (actions.Count > 0) {
            var response = await SendRequest(RequestBuilder.RequestAction(actions));

            var unsuccessfulActions = actions
                .Zip(response.Action.Result, (action, result) => (action, result))
                .Where(action => action.result != ActionResult.Success)
                .Select(action => $"({KnowledgeBase.GetAbilityData(action.action.ActionRaw.UnitCommand.AbilityId).FriendlyName}, {action.result})")
                .ToList();

            if (unsuccessfulActions.Count > 0) {
                Logger.Warning("Unsuccessful actions: [{0}]", string.Join("; ", unsuccessfulActions));
            }
        }
        _performanceDebugger.ActionsStopwatch.Stop();

        _performanceDebugger.DebuggerStopwatch.Start();
        var request = Program.GraphicalDebugger.GetDebugRequest();
        if (request != null) {
            await SendRequest(request);
        }
        _performanceDebugger.DebuggerStopwatch.Stop();

        _performanceDebugger.FrameStopwatch.Stop();

        if (_performanceDebugger.FrameStopwatch.ElapsedMilliseconds > 10) {
            _performanceDebugger.LogTimers(actions.Count);
        }

        _performanceDebugger.CompileData();
    }

    private static void PrintMemoryInfo() {
        var memoryUsedMb = Process.GetCurrentProcess().WorkingSet64 * 1e-6;
        if (memoryUsedMb > 200) {
            Logger.Performance("==== Memory Debug Start ====");
            Logger.Performance("Memory used: {0} MB", memoryUsedMb.ToString("0.00"));
            Logger.Performance("Units: {0} owned, {1} neutral, {2} enemy", UnitsTracker.OwnedUnits.Count, UnitsTracker.NeutralUnits.Count, UnitsTracker.EnemyUnits.Count);
            Logger.Performance(
                "Pathfinding cache: {0} paths, {1} tiles",
                Pathfinder.CellPathsMemory.Values.Sum(destinations => destinations.Keys.Count),
                Pathfinder.CellPathsMemory.Values.SelectMany(destinations => destinations.Values).Sum(path => path.Count));
            Logger.Performance("==== Memory Debug End ====");
        }
    }

    private static void LogResponseErrors(Response response) {
        if (response.Error.Count > 0) {
            Logger.Error("Response errors:");
            foreach (var error in response.Error) {
                Logger.Error(error);
            }
        }
    }

    public Task Quit() {
        Logger.Info("Quitting game...");
        return SendRequest(new Request
        {
            Quit = new RequestQuit()
        });
    }

    public async Task<Response> SendRequest(Request request, bool logErrors = false) {
        var response = await _proxy.SendRequest(request);
        if (logErrors) {
            LogResponseErrors(response);
        }

        return response;
    }
}
