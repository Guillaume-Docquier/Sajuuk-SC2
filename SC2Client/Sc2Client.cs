using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using Google.Protobuf;
using SC2APIProtocol;

namespace SC2Client;

[ExcludeFromCodeCoverage]
public sealed class Sc2Client : ISc2Client, IDisposable {
    private const int ConnectTimeout = 20000;
    private const int ReadWriteTimeout = 120000;

    private readonly ILogger _logger;
    private readonly GameDisplayMode _gameDisplayMode;

    private ClientWebSocket? _clientWebsocket;

    public Sc2Client(ILogger logger, GameDisplayMode gameDisplayMode) {
        _logger = logger;
        _gameDisplayMode = gameDisplayMode;
    }

    public void LaunchSc2(string serverAddress, int gamePort) {
        _logger.Info("Finding the SC2 executable info");
        var executableInfo = FindExecutableInfo();

        _logger.Info("Launching SC2 instance");
        StartGameClient(serverAddress, gamePort, executableInfo.sc2ExeFilePath, _gameDisplayMode);
    }

    private static (string sc2ExeFilePath, string sc2MapsDirectoryPath) FindExecutableInfo() {
        var myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var executeInfoFilePath = Path.Combine(myDocuments, "StarCraft II", "ExecuteInfo.txt");
        if (!File.Exists(executeInfoFilePath)) {
            throw new Exception($"Unable to find:{executeInfoFilePath}. Make sure you started the game successfully at least once.");
        }

        var executeInfo = File.ReadAllLines(executeInfoFilePath);
        var sc2ExeFilePathInfo = executeInfo.FirstOrDefault(line => line.Trim().StartsWith("executable"));
        if (sc2ExeFilePathInfo == null) {
            throw new Exception($"Unable to find the executable path from executable info: [\n{string.Join('\n', executeInfo)}\n]");
        }

        var sc2ExeFilePath = sc2ExeFilePathInfo.Substring(sc2ExeFilePathInfo.IndexOf('=') + 1).Trim();

        var sc2RootDir = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(sc2ExeFilePath))); // We need 2 folders down
        if (sc2RootDir == null) {
            throw new Exception($"Unable to find the sc2 directory from the file path: {sc2ExeFilePath}");
        }

        var sc2MapsDirectoryPath = Path.Combine(sc2RootDir, "Maps");

        return (sc2ExeFilePath, sc2MapsDirectoryPath);
    }

    private void StartGameClient(string serverAddress, int gamePort, string sc2ExeFilePath, GameDisplayMode gameDisplayMode) {
        var processStartInfo = new ProcessStartInfo(sc2ExeFilePath)
        {
            Arguments = $"-listen {serverAddress} -port {gamePort} -displayMode {gameDisplayMode}",
            WorkingDirectory = Path.Combine(sc2ExeFilePath, "Support64")
        };

        _logger.Debug($"Starting game client with arguments: {processStartInfo.Arguments}");
        Process.Start(processStartInfo);
    }

    public async Task ConnectToGameClient(string serverAddress, int gamePort, int maxRetries = 60) {
        _logger.Info($"Attempting to connect to SC2 instance at {serverAddress}:{gamePort}");

        for (var retryCount = 0; retryCount < maxRetries; retryCount++) {
            try {
                await TryConnect(serverAddress, gamePort);
                _logger.Info("Connected to SC2 instance");

                return;
            }
            catch (WebSocketException) {
                _logger.Debug($"[{retryCount}/{maxRetries}] Connection failed. Retrying in 1 second...");
            }

            Thread.Sleep(1000);
        }

        throw new Exception($"Unable to connect to SC2 after {maxRetries} retries.");
    }

    private async Task TryConnect(string serverAddress, int gamePort) {
        _clientWebsocket = new ClientWebSocket();
        // Disable PING control frames (https://tools.ietf.org/html/rfc6455#section-5.5.2).
        // It seems SC2 built in websocket server does not do PONG but tries to process ping as
        // request and then sends empty response to client.
        _clientWebsocket.Options.KeepAliveInterval = TimeSpan.FromDays(30);

        var websocketUri = new Uri($"ws://{serverAddress}:{gamePort}/sc2api");
        using (var cancellationSource = new CancellationTokenSource()) {
            cancellationSource.CancelAfter(ConnectTimeout);
            await _clientWebsocket.ConnectAsync(websocketUri, cancellationSource.Token);
        }

        await Ping();
    }

    public async Task CreateGame(string mapFileName, Race opponentRace, Difficulty opponentDifficulty, bool realTime) {
        _logger.Info("Finding the SC2 executable info");
        var executableInfo = FindExecutableInfo();

        _logger.Info($"Creating game on map: {mapFileName}");
        var mapPath = Path.Combine(executableInfo.sc2MapsDirectoryPath, mapFileName);
        if (!File.Exists(mapPath)) {
            throw new Exception($"Unable to locate map: {mapPath}");
        }

        var createGameResponse = await SendRequest(RequestBuilder.RequestCreateComputerGame(realTime, mapPath, opponentRace, opponentDifficulty));

        // TODO GD This might be broken now, used to be ResponseJoinGame.Types.Error.Unset (0) but it doesn't exist anymore
        if (createGameResponse.CreateGame.Error != ResponseCreateGame.Types.Error.MissingMap) {
            _logger.Error($"CreateGame error: {createGameResponse.CreateGame.Error.ToString()}");
            if (!string.IsNullOrEmpty(createGameResponse.CreateGame.ErrorDetails)) {
                _logger.Error(createGameResponse.CreateGame.ErrorDetails);
            }

            throw new Exception("Could not create game, see the logs for more info");
        }
    }

    public async Task<uint> JoinGame(Request joinGameRequest) {
        var joinGameResponse = await SendRequest(joinGameRequest);

        // TODO GD This might be broken now, used to be ResponseJoinGame.Types.Error.Unset (0) but it doesn't exist anymore
        if (joinGameResponse.JoinGame.Error != ResponseJoinGame.Types.Error.MissingParticipation) {
            _logger.Error($"JoinGame error: {joinGameResponse.JoinGame.Error.ToString()}");
            if (!string.IsNullOrEmpty(joinGameResponse.JoinGame.ErrorDetails)) {
                _logger.Error(joinGameResponse.JoinGame.ErrorDetails);
            }

            throw new Exception("Could not join game, see the logs for more info");
        }

        return joinGameResponse.JoinGame.PlayerId;
    }

    public Task LeaveCurrentGame() {
        _logger.Info("Quitting game...");
        return SendRequest(RequestBuilder.RequestQuitGame());
    }

    private async Task Ping() {
        await SendRequest(new Request
        {
            Ping = new RequestPing()
        });
    }

    public async Task<Response> SendRequest(Request request) {
        if (_clientWebsocket == null) {
            throw new Exception("Cannot send request before the connection to SC2 is initialized. The client websocket is null.");
        }

        await WriteRequest(request, _clientWebsocket);

        return await ReadResponse(_clientWebsocket);
    }

    /**
     * Sends the protobuf request through the websocket.
     */
    private static async Task WriteRequest(IMessage request, WebSocket websocket) {
        var sendBuffer = ArrayPool<byte>.Shared.Rent(1024 * 1024);
        var outStream = new CodedOutputStream(sendBuffer);
        request.WriteTo(outStream);

        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.CancelAfter(ReadWriteTimeout);
        await websocket.SendAsync(
            new ArraySegment<byte>(sendBuffer, 0, (int)outStream.Position),
            WebSocketMessageType.Binary,
            true,
            cancellationSource.Token
        );
        ArrayPool<byte>.Shared.Return(sendBuffer);
    }

    /**
     * Reads a response through the websocket after writing a request.
     */
    private static async Task<Response> ReadResponse(WebSocket websocket) {
        var receiveBuffer = ArrayPool<byte>.Shared.Rent(1024 * 1024);
        var responseLength = 0;
        var hasReachedEndOfMessage = false;

        while (!hasReachedEndOfMessage) {
            var bufferSpaceLeft = receiveBuffer.Length - responseLength;
            if (bufferSpaceLeft < 0) {
                var largerReceiveBuffer = ArrayPool<byte>.Shared.Rent(receiveBuffer.Length * 2);
                Array.Copy(receiveBuffer, largerReceiveBuffer, receiveBuffer.Length);
                ArrayPool<byte>.Shared.Return(receiveBuffer);

                receiveBuffer = largerReceiveBuffer;
                bufferSpaceLeft = receiveBuffer.Length - responseLength;
            }

            using var cancellationSource = new CancellationTokenSource();
            cancellationSource.CancelAfter(ReadWriteTimeout);
            var result = await websocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer, responseLength, bufferSpaceLeft), cancellationSource.Token);

            responseLength += result.Count;
            hasReachedEndOfMessage = result.EndOfMessage;
        }

        var response = Response.Parser.ParseFrom(new MemoryStream(receiveBuffer, 0, responseLength));
        ArrayPool<byte>.Shared.Return(receiveBuffer);

        return response;
    }

    public void Dispose() {
        _clientWebsocket?.Dispose();
    }
}
