using System;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using SC2APIProtocol;

namespace Bot.Wrapper {
    public class GameConnection {
        private const string Address = "127.0.0.1";
        private const int StepSize = 1;
        private readonly ProtobufProxy _proxy = new ProtobufProxy();
        private string _starcraftDir;

        private string _starcraftExe;
        private string _starcraftMaps;

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

        private async Task CreateGame(string mapName, Race opponentRace, Difficulty opponentDifficulty, bool realTime) {
            var createGame = new RequestCreateGame
            {
                Realtime = realTime
            };

            var mapPath = Path.Combine(_starcraftMaps, mapName);

            if (!File.Exists(mapPath)) {
                Logger.Info($"Unable to locate map: {mapPath}");
                throw new Exception($"Unable to locate map: {mapPath}");
            }

            createGame.LocalMap = new LocalMap
            {
                MapPath = mapPath
            };

            var player1 = new PlayerSetup();
            createGame.PlayerSetup.Add(player1);
            player1.Type = PlayerType.Participant;

            var player2 = new PlayerSetup();
            createGame.PlayerSetup.Add(player2);
            player2.Race = opponentRace;
            player2.Type = PlayerType.Computer;
            player2.Difficulty = opponentDifficulty;

            var request = new Request
            {
                CreateGame = createGame
            };

            var response = CheckResponse(await _proxy.SendRequest(request));
            if (response.CreateGame.Error != ResponseCreateGame.Types.Error.Unset) {
                Logger.Error("CreateGame error: {0}", response.CreateGame.Error.ToString());
                if (!string.IsNullOrEmpty(response.CreateGame.ErrorDetails)) {
                    Logger.Error(response.CreateGame.ErrorDetails);
                }
            }
        }

        public void FindExecutablePath() {
            var myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var executeInfo = Path.Combine(myDocuments, "StarCraft II", "ExecuteInfo.txt");
            if (!File.Exists(executeInfo)) {
                executeInfo = Path.Combine(myDocuments, "StarCraftII", "ExecuteInfo.txt");
            }

            if (File.Exists(executeInfo)) {
                var lines = File.ReadAllLines(executeInfo);
                foreach (var line in lines) {
                    if (line.Trim().StartsWith("executable")) {
                        _starcraftExe = line.Substring(line.IndexOf('=') + 1).Trim();
                        ;
                        _starcraftDir = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(_starcraftExe))); //we need 2 folders down
                        if (_starcraftDir != null) {
                            _starcraftMaps = Path.Combine(_starcraftDir, "Maps");
                        }
                    }
                }
            }
            else {
                throw new Exception($"Unable to find:{executeInfo}. Make sure you started the game successfully at least once.");
            }
        }

        private async Task<uint> JoinGame(Race race) {
            var joinGame = new RequestJoinGame();
            joinGame.Race = race;

            joinGame.Options = new InterfaceOptions();
            joinGame.Options.Raw = true;
            joinGame.Options.Score = true;

            var request = new Request();
            request.JoinGame = joinGame;
            var response = CheckResponse(await _proxy.SendRequest(request));

            if (response.JoinGame.Error != ResponseJoinGame.Types.Error.Unset) {
                Logger.Error("JoinGame error: {0}", response.JoinGame.Error.ToString());
                if (!String.IsNullOrEmpty(response.JoinGame.ErrorDetails)) {
                    Logger.Error(response.JoinGame.ErrorDetails);
                }
            }

            return response.JoinGame.PlayerId;
        }

        private async Task<uint> JoinGameLadder(Race race, int startPort) {
            var joinGame = new RequestJoinGame();
            joinGame.Race = race;

            joinGame.SharedPort = startPort + 1;
            joinGame.ServerPorts = new PortSet();
            joinGame.ServerPorts.GamePort = startPort + 2;
            joinGame.ServerPorts.BasePort = startPort + 3;

            joinGame.ClientPorts.Add(new PortSet());
            joinGame.ClientPorts[0].GamePort = startPort + 4;
            joinGame.ClientPorts[0].BasePort = startPort + 5;

            joinGame.Options = new InterfaceOptions();
            joinGame.Options.Raw = true;
            joinGame.Options.Score = true;

            var request = new Request();
            request.JoinGame = joinGame;

            var response = CheckResponse(await _proxy.SendRequest(request));

            if (response.JoinGame.Error != ResponseJoinGame.Types.Error.Unset) {
                Logger.Error("JoinGame error: {0}", response.JoinGame.Error.ToString());
                if (!String.IsNullOrEmpty(response.JoinGame.ErrorDetails)) {
                    Logger.Error(response.JoinGame.ErrorDetails);
                }
            }

            return response.JoinGame.PlayerId;
        }

        public async Task Ping() {
            await _proxy.Ping();
        }

        private async Task RequestLeaveGame() {
            var requestLeaveGame = new Request();
            requestLeaveGame.LeaveGame = new RequestLeaveGame();
            await _proxy.SendRequest(requestLeaveGame);
        }

        public async Task SendRequest(Request request) {
            await _proxy.SendRequest(request);
        }

        public async Task<ResponseQuery> SendQuery(RequestQuery query) {
            var request = new Request();
            request.Query = query;
            var response = await _proxy.SendRequest(request);
            return response.Query;
        }

        private async Task Run(IBot bot, uint playerId) {
            var gameInfoReq = new Request();
            gameInfoReq.GameInfo = new RequestGameInfo();

            var gameInfoResponse = await _proxy.SendRequest(gameInfoReq);

            var dataReq = new Request();
            dataReq.Data = new RequestData();
            dataReq.Data.UnitTypeId = true;
            dataReq.Data.AbilityId = true;
            dataReq.Data.BuffId = true;
            dataReq.Data.EffectId = true;
            dataReq.Data.UpgradeId = true;

            var dataResponse = await _proxy.SendRequest(dataReq);

            Controller.GameInfo = gameInfoResponse.GameInfo;
            Controller.GameData = dataResponse.Data;

            while (true) {
                var observationRequest = new Request();
                observationRequest.Observation = new RequestObservation();
                var response = await _proxy.SendRequest(observationRequest);

                var observation = response.Observation;

                if (response.Status == Status.Ended || response.Status == Status.Quit) {
                    foreach (var result in observation.PlayerResult) {
                        if (result.PlayerId == playerId) {
                            Logger.Info("Result: {0}", result.Result);
                            // Do whatever you want with the info
                        }
                    }

                    break;
                }

                Controller.Obs = observation;
                var actions = bot.OnFrame();

                var actionRequest = new Request();
                actionRequest.Action = new RequestAction();
                actionRequest.Action.Actions.AddRange(actions);
                if (actionRequest.Action.Actions.Count > 0)
                    await _proxy.SendRequest(actionRequest);

                var stepRequest = new Request();
                stepRequest.Step = new RequestStep();
                stepRequest.Step.Count = StepSize;
                await _proxy.SendRequest(stepRequest);
            }
        }

        public async Task RunSinglePlayer(IBot bot, string map, Race myRace, Race opponentRace, Difficulty opponentDifficulty, bool realTime) {
            const int port = 5678;

            Logger.Info("Starting SinglePlayer Instance");
            StartSc2Instance(port);

            Logger.Info("Connecting to port: {0}", port);
            await Connect(port);

            Logger.Info("Creating game");
            await CreateGame(map, opponentRace, opponentDifficulty, realTime);

            Logger.Info("Joining game");
            var playerId = await JoinGame(myRace);
            await Run(bot, playerId);
        }

        private async Task RunLadder(IBot bot, Race myRace, int gamePort, int startPort) {
            await Connect(gamePort);
            var playerId = await JoinGameLadder(myRace, startPort);
            await Run(bot, playerId);
            // await RequestLeaveGame();
        }

        public async Task RunLadder(IBot bot, Race myRace, string[] args) {
            var commandLineArgs = new CommandLineArguments(args);
            await RunLadder(bot, myRace, commandLineArgs.GamePort, commandLineArgs.StartPort);
        }

        private static Response CheckResponse(Response response) {
            if (response.Error.Count > 0) {
                Logger.Error("Response errors:");
                foreach (var error in response.Error) {
                    Logger.Error(error);
                }
            }

            return response;
        }
    }
}
