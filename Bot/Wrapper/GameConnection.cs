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
using SC2APIProtocol;

namespace Bot.Wrapper;

public class GameConnection {
    private const string Address = "127.0.0.1";
    private const int StepSize = 1;
    private readonly ProtobufProxy _proxy = new ProtobufProxy();

    private string _starcraftExe;
    private string _starcraftDir;
    private string _starcraftMapsDir;

    private readonly ulong _runEvery;
    private static readonly ulong DebugMemoryEvery = Controller.SecsToFrames(5);

    private readonly PerformanceDebugger _performanceDebugger = new PerformanceDebugger();

    public GameConnection(ulong runEvery) {
        _runEvery = runEvery;
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

    public async Task RunSinglePlayer(IBot bot, string mapName, Race opponentRace, Difficulty opponentDifficulty, bool realTime) {
        const int port = 5678;

        Logger.Info("Finding the SC2 executable info");
        FindExecutableInfo();

        Logger.Info("Starting SinglePlayer Instance");
        StartSc2Instance(port);

        Logger.Info("Connecting to port: {0}", port);
        await Connect(port);

        Logger.Info("Creating game on map: {0}", mapName);
        await CreateGame(mapName, opponentRace, opponentDifficulty, realTime);

        Logger.Info("Joining game");
        var playerId = await JoinGame(bot.Race);
        await Run(bot, playerId);
    }

    private void StartSc2Instance(int port) {
        var processStartInfo = new ProcessStartInfo(_starcraftExe)
        {
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
                Logger.Info("Failed. Retrying...");
            }

            Thread.Sleep(500);
        }

        Logger.Info("Unable to connect to SC2 after {0} seconds.", timeout);
        throw new Exception("Unable to make a connection.");
    }

    private async Task CreateGame(string mapName, Race opponentRace, Difficulty opponentDifficulty, bool realTime) {;
        var mapPath = Path.Combine(_starcraftMapsDir, mapName);
        if (!File.Exists(mapPath)) {
            Logger.Info($"Unable to locate map: {mapPath}");
            throw new Exception($"Unable to locate map: {mapPath}");
        }

        var createGameResponse = await SendRequest(RequestBuilder.RequestCreateComputerGame(realTime, mapPath, opponentRace, opponentDifficulty), logErrors: true);

        if (createGameResponse.CreateGame.Error != ResponseCreateGame.Types.Error.Unset) {
            Logger.Error("CreateGame error: {0}", createGameResponse.CreateGame.Error.ToString());
            if (!string.IsNullOrEmpty(createGameResponse.CreateGame.ErrorDetails)) {
                Logger.Error(createGameResponse.CreateGame.ErrorDetails);
            }
        }
    }

    private async Task<uint> JoinGame(Race race) {
        var joinGameResponse = await SendRequest(RequestBuilder.RequestJoinLocalGame(race), logErrors: true);

        if (joinGameResponse.JoinGame.Error != ResponseJoinGame.Types.Error.Unset) {
            Logger.Error("JoinGame error: {0}", joinGameResponse.JoinGame.Error.ToString());
            if (!String.IsNullOrEmpty(joinGameResponse.JoinGame.ErrorDetails)) {
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

        if (joinGameResponse.JoinGame.Error != ResponseJoinGame.Types.Error.Unset) {
            Logger.Error("JoinGame error: {0}", joinGameResponse.JoinGame.Error.ToString());
            if (!string.IsNullOrEmpty(joinGameResponse.JoinGame.ErrorDetails)) {
                Logger.Error(joinGameResponse.JoinGame.ErrorDetails);
            }
        }

        return joinGameResponse.JoinGame.PlayerId;
    }

    private async Task Run(IBot bot, uint playerId) {
        var dataRequest = new Request
        {
            Data = new RequestData
            {
                UnitTypeId = true,
                AbilityId = true,
                BuffId = true,
                EffectId = true,
                UpgradeId = true
            }
        };
        var dataResponse = await SendRequest(dataRequest);
        KnowledgeBase.Data = dataResponse.Data;

        while (true) {
            var observationResponse = await SendRequest(RequestBuilder.RequestObservation());

            var observation = observationResponse.Observation;
            if (observationResponse.Status is Status.Ended or Status.Quit) {
                _performanceDebugger.LogAveragePerformance();

                foreach (var result in observation.PlayerResult) {
                    if (result.PlayerId == playerId) {
                        Logger.Info("Result: {0}", result.Result);
                    }
                }

                break;
            }

            // Can happen when realTime == true
            if (Controller.Frame == observation.Observation.GameLoop) {
                continue;
            }

            if (observation.Observation.GameLoop % _runEvery == 0) {
                var gameInfoResponse = await SendRequest(RequestBuilder.RequestGameInfo());

                _performanceDebugger.FrameStopwatch.Start();
                _performanceDebugger.ControllerStopwatch.Start();
                Controller.NewGameInfo(gameInfoResponse.GameInfo);
                Controller.NewObservation(observation);
                _performanceDebugger.ControllerStopwatch.Stop();

                _performanceDebugger.BotStopwatch.Start();
                bot.OnFrame();
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
                GraphicalDebugger.SendDebugRequest();
                _performanceDebugger.DebuggerStopwatch.Stop();
                _performanceDebugger.FrameStopwatch.Stop();

                if (_performanceDebugger.FrameStopwatch.ElapsedMilliseconds > 10) {
                    _performanceDebugger.LogTimers(actions.Count);
                }

                _performanceDebugger.CompileData();
            }

            if (observation.Observation.GameLoop % DebugMemoryEvery == 0) {
                PrintMemoryInfo();
            }

            await SendRequest(RequestBuilder.RequestStep(StepSize));
        }
    }

    private static void PrintMemoryInfo() {
        var memoryUsedMb = Process.GetCurrentProcess().WorkingSet64 * 1e-6;
        if (memoryUsedMb > 200) {
            Logger.Info("==== Memory Debug Start ====");
            Logger.Info("Memory used: {0} MB", memoryUsedMb.ToString("0.00"));
            Logger.Info("Units: {0} owned, {1} neutral, {2} enemy", UnitsTracker.OwnedUnits.Count, UnitsTracker.NeutralUnits.Count, UnitsTracker.EnemyUnits.Count);
            Logger.Info(
                "Pathfinding cache: {0} paths, {1} tiles",
                Pathfinder.Memory.Values.Sum(destinations => destinations.Keys.Count),
                Pathfinder.Memory.Values.SelectMany(destinations => destinations.Values).Sum(path => path.Count));
            Logger.Info("==== Memory Debug End ====");
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

    private Task RequestLeaveGame() {
        return SendRequest(new Request
        {
            LeaveGame = new RequestLeaveGame()
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
