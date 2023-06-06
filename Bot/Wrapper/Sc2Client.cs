using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using SC2APIProtocol;

namespace Bot.Wrapper;

[ExcludeFromCodeCoverage]
public class Sc2Client : ISc2Client {
    private readonly IRequestBuilder _requestBuilder;

    private ClientWebSocket _clientSocket; // TODO GD Do we need to dispose of the websocket?
    private const int ConnectTimeout = 20000;
    private const int ReadWriteTimeout = 120000;

    // TODO GD Group these 3 into a struct and extract the FindExecutableInfo from here?
    private string _starcraftExe;
    private string _starcraftDir;
    private string _starcraftMapsDir;

    public Sc2Client(IRequestBuilder requestBuilder) {
        _requestBuilder = requestBuilder;
    }

    public Task LaunchSc2(string serverAddress, int gamePort) {
        Logger.Info("Finding the SC2 executable info");
        FindExecutableInfo();

        Logger.Info("Launching SC2 instance");
        StartInstance(serverAddress, gamePort);

        return Task.CompletedTask;
    }

    private void StartInstance(string serverAddress, int gamePort) {
        var processStartInfo = new ProcessStartInfo(_starcraftExe)
        {
            // TODO GD Make and enum for this
            // DisplayMode 0: Windowed
            // DisplayMode 1: Full screen
            Arguments = $"-listen {serverAddress} -port {gamePort} -displayMode 1",
            WorkingDirectory = Path.Combine(_starcraftDir, "Support64")
        };

        Logger.Debug($"SC2.exe: {_starcraftExe}");
        Logger.Debug($"Working Dir: {processStartInfo.WorkingDirectory}");
        Logger.Debug($"Arguments: {processStartInfo.Arguments}");
        Process.Start(processStartInfo);
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

    public async Task Connect(string serverAddress, int gamePort, int maxRetries = 60) {
        Logger.Info($"Attempting to connect to SC2 instance at {serverAddress}:{gamePort}");

        for (var i = 0; i < maxRetries; i++) {
            try {
                await TryConnect(serverAddress, gamePort);
                Logger.Info("Connected to SC2 instance");

                return;
            }
            catch (WebSocketException) {
                Logger.Debug("Failed. Retrying...");
            }

            Thread.Sleep(1000);
        }

        Logger.Error($"Unable to connect to SC2 after {maxRetries} retries.");
        throw new Exception("Unable to make a connection.");
    }

    private async Task TryConnect(string serverAddress, int gamePort) {
        _clientSocket = new ClientWebSocket();
        // Disable PING control frames (https://tools.ietf.org/html/rfc6455#section-5.5.2).
        // It seems SC2 built in websocket server does not do PONG but tries to process ping as
        // request and then sends empty response to client.
        _clientSocket.Options.KeepAliveInterval = TimeSpan.FromDays(30);

        var websocketUri = new Uri($"ws://{serverAddress}:{gamePort}/sc2api");
        using (var cancellationSource = new CancellationTokenSource()) {
            cancellationSource.CancelAfter(ConnectTimeout);
            await _clientSocket.ConnectAsync(websocketUri, cancellationSource.Token);
        }

        await Ping();
    }

    public async Task<Response> SendRequest(Request request, bool logErrors = false) {
        await WriteMessage(request);

        var response = await ReadMessage();
        if (logErrors) {
            LogResponseErrors(response);
        }

        return response;
    }

    private static void LogResponseErrors(Response response) {
        if (response.Error.Count > 0) {
            Logger.Error("Response errors:");
            foreach (var error in response.Error) {
                Logger.Error(error);
            }
        }
    }

    public async Task CreateGame(string mapFileName, Race opponentRace, Difficulty opponentDifficulty, bool realTime) {
        Logger.Info($"Creating game on map: {mapFileName}");

        var mapPath = Path.Combine(_starcraftMapsDir, mapFileName);
        if (!File.Exists(mapPath)) {
            Logger.Error($"Unable to locate map: {mapPath}");
            throw new Exception($"Unable to locate map: {mapPath}");
        }

        var createGameResponse = await SendRequest(_requestBuilder.RequestCreateComputerGame(realTime, mapPath, opponentRace, opponentDifficulty), logErrors: true);

        // TODO GD This might be broken now, used to be ResponseJoinGame.Types.Error.Unset (0) but it doesn't exist anymore
        if (createGameResponse.CreateGame.Error != ResponseCreateGame.Types.Error.MissingMap) {
            Logger.Error($"CreateGame error: {createGameResponse.CreateGame.Error.ToString()}");
            if (!string.IsNullOrEmpty(createGameResponse.CreateGame.ErrorDetails)) {
                Logger.Error(createGameResponse.CreateGame.ErrorDetails);
            }
        }
    }

    public Task<uint> JoinLocalGame(Race race) {
        Logger.Info("Joining local game");
        return JoinGame(_requestBuilder.RequestJoinLocalGame(race));
    }

    public Task<uint> JoinLadderGame(Race race, int startPort) {
        Logger.Info("Joining ladder game");
        return JoinGame(_requestBuilder.RequestJoinLadderGame(race, startPort));
    }

    private async Task<uint> JoinGame(Request joinGameRequest) {
        var joinGameResponse = await SendRequest(joinGameRequest, logErrors: true);

        // TODO GD This might be broken now, used to be ResponseJoinGame.Types.Error.Unset (0) but it doesn't exist anymore
        if (joinGameResponse.JoinGame.Error != ResponseJoinGame.Types.Error.MissingParticipation) {
            Logger.Error($"JoinGame error: {joinGameResponse.JoinGame.Error.ToString()}");
            if (!string.IsNullOrEmpty(joinGameResponse.JoinGame.ErrorDetails)) {
                Logger.Error(joinGameResponse.JoinGame.ErrorDetails);
            }
        }

        return joinGameResponse.JoinGame.PlayerId;
    }

    public Task LeaveCurrentGame() {
        Logger.Info("Quitting game...");
        return SendRequest(new Request
        {
            Quit = new RequestQuit()
        });
    }

    private async Task Ping() {
        await SendRequest(new Request
        {
            Ping = new RequestPing()
        });
    }

    private async Task WriteMessage(IMessage request) {
        var sendBuf = ArrayPool<byte>.Shared.Rent(1024 * 1024);
        var outStream = new CodedOutputStream(sendBuf);
        request.WriteTo(outStream);

        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.CancelAfter(ReadWriteTimeout);
        await _clientSocket.SendAsync(
            new ArraySegment<byte>(sendBuf, 0, (int)outStream.Position),
            WebSocketMessageType.Binary,
            true,
            cancellationSource.Token
        );
        ArrayPool<byte>.Shared.Return(sendBuf);
    }

    private async Task<Response> ReadMessage() {
        var receiveBuf = ArrayPool<byte>.Shared.Rent(1024 * 1024);
        var finished = false;
        var curPos = 0;

        while (!finished) {
            using var cancellationSource = new CancellationTokenSource();
            var left = receiveBuf.Length - curPos;
            if (left < 0) {
                // No space left in the array, enlarge the array by doubling its size.
                var temp = new byte[receiveBuf.Length * 2];
                Array.Copy(receiveBuf, temp, receiveBuf.Length);
                ArrayPool<byte>.Shared.Return(receiveBuf);
                receiveBuf = temp;
                left = receiveBuf.Length - curPos;
            }

            cancellationSource.CancelAfter(ReadWriteTimeout);
            var result = await _clientSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuf, curPos, left), cancellationSource.Token);
            if (result.MessageType != WebSocketMessageType.Binary) {
                throw new Exception("Expected Binary message type.");
            }

            curPos += result.Count;
            finished = result.EndOfMessage;
        }

        var response = Response.Parser.ParseFrom(new MemoryStream(receiveBuf, 0, curPos));
        ArrayPool<byte>.Shared.Return(receiveBuf);
        return response;
    }
}
