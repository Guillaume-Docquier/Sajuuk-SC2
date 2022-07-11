using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using SC2APIProtocol;

namespace Bot.Wrapper;

public class GameConnection {
    private const string Address = "127.0.0.1";
    private const int StepSize = 1;
    private readonly ProtobufProxy _proxy = new ProtobufProxy();

    private readonly string _starcraftExe;
    private readonly string _starcraftDir;
    private readonly string _starcraftMapsDir;

    public GameConnection() {
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
            Arguments = $"-listen {Address} -port {port} -displayMode 0",
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

        var createGame = new RequestCreateGame
        {
            Realtime = realTime,
            LocalMap = new LocalMap
            {
                MapPath = mapPath
            }
        };

        createGame.PlayerSetup.Add(new PlayerSetup
        {
            Type = PlayerType.Participant
        });

        createGame.PlayerSetup.Add(new PlayerSetup
        {
            Type = PlayerType.Computer,
            Race = opponentRace,
            Difficulty = opponentDifficulty
        });

        var createGameRequest = new Request
        {
            CreateGame = createGame
        };
        var createGameResponse = await SendRequest(createGameRequest, logErrors: true);

        if (createGameResponse.CreateGame.Error != ResponseCreateGame.Types.Error.Unset) {
            Logger.Error("CreateGame error: {0}", createGameResponse.CreateGame.Error.ToString());
            if (!string.IsNullOrEmpty(createGameResponse.CreateGame.ErrorDetails)) {
                Logger.Error(createGameResponse.CreateGame.ErrorDetails);
            }
        }
    }

    private async Task<uint> JoinGame(Race race) {
        var joinGame = new RequestJoinGame
        {
            Race = race,
            Options = new InterfaceOptions
            {
                Raw = true,
                Score = true
            }
        };

        var joinGameRequest = new Request
        {
            JoinGame = joinGame
        };
        var joinGameResponse = await SendRequest(joinGameRequest, logErrors: true);

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

    public async Task<ResponseQuery> SendQuery(RequestQuery query) {
        var response = await SendRequest(new Request
        {
            Query = query
        });

        return response.Query;
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
        var joinGame = new RequestJoinGame
        {
            Race = race,
            SharedPort = startPort + 1,
            ServerPorts = new PortSet
            {
                GamePort = startPort + 2,
                BasePort = startPort + 3
            },
            Options = new InterfaceOptions
            {
                Raw = true,
                Score = true
            }
        };

        joinGame.ClientPorts.Add(new PortSet
        {
            GamePort = startPort + 4,
            BasePort = startPort + 5
        });

        var joinGameRequest = new Request
        {
            JoinGame = joinGame
        };
        var joinGameResponse = await SendRequest(joinGameRequest, logErrors: true);

        if (joinGameResponse.JoinGame.Error != ResponseJoinGame.Types.Error.Unset) {
            Logger.Error("JoinGame error: {0}", joinGameResponse.JoinGame.Error.ToString());
            if (!string.IsNullOrEmpty(joinGameResponse.JoinGame.ErrorDetails)) {
                Logger.Error(joinGameResponse.JoinGame.ErrorDetails);
            }
        }

        return joinGameResponse.JoinGame.PlayerId;
    }

    private async Task Run(IBot bot, uint playerId) {
        var gameInfoRequest = new Request
        {
            GameInfo = new RequestGameInfo()
        };
        var gameInfoResponse = await SendRequest(gameInfoRequest);
        Controller.GameInfo = gameInfoResponse.GameInfo;

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
        GameData.Data = dataResponse.Data;

        while (true) {
            var observationResponse = await SendRequest(new Request
            {
                Observation = new RequestObservation()
            });

            var observation = observationResponse.Observation;

            if (observationResponse.Status is Status.Ended or Status.Quit) {
                foreach (var result in observation.PlayerResult) {
                    if (result.PlayerId == playerId) {
                        Logger.Info("Result: {0}", result.Result);
                        // Do whatever you want with the info
                    }
                }

                break;
            }

            // Can happen when realTime == true
            if (Controller.Frame == observation.Observation.GameLoop) {
                continue;
            }

            // TODO GD Init code (controller & map analysis)
            // TODO GD Tick controller
            // TODO GD Tick bot
            Controller.Obs = observation;

            var actions = bot.OnFrame().ToList();

            // For some reason it doesn't work before a few seconds after the game starts
            // Also, this might take a couple of frames, let the bot start the game
            // TODO GD Precompute this and save it
            if (Controller.Frame > Controller.FramesPerSecond * 5 && !MapAnalyzer.IsInitialized) {
                MapAnalyzer.Init();
            }

            if (actions.Count > 0) {
                var response = await SendRequest(RequestBuilder.ActionRequest(actions));

                var unsuccessfulActions = actions
                    .Zip(response.Action.Result, (action, result) => (action, result))
                    .Where(action => action.result != ActionResult.Success)
                    .Select(action => $"({action.action.ActionRaw.UnitCommand.AbilityId}, {action.result})")
                    .ToList();

                if (unsuccessfulActions.Count > 0) {
                    Logger.Warning("Unsuccessful actions: [{0}]", string.Join("; ", unsuccessfulActions));
                }
            }

            await SendRequest(Debugger.GetDebugRequest());
            await SendRequest(RequestBuilder.StepRequest(StepSize));
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

    private async Task<Response> SendRequest(Request request, bool logErrors = false) {
        var response = await _proxy.SendRequest(request);
        if (logErrors) {
            LogResponseErrors(response);
        }

        return response;
    }
}
