using System;
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
    private ClientWebSocket _clientSocket; // TODO GD Do we need to dispose of the websocket?
    private const int ConnectTimeout = 20000;
    private const int ReadWriteTimeout = 120000;

    public async Task Connect(string serverAddress, int gamePort, int maxRetries = 60) {
        Logger.Info($"Attempting to connect to Sc2 instance at {serverAddress}:{gamePort}");

        for (var i = 0; i < maxRetries; i++) {
            try {
                await TryConnect(serverAddress, gamePort);
                Logger.Info("--> Connected");

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

    public async Task<Response> SendRequest(Request request) {
        await WriteMessage(request);

        return await ReadMessage();
    }

    public Task Quit() {
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
        var sendBuf = new byte[1024 * 1024];
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
    }

    private async Task<Response> ReadMessage() {
        var receiveBuf = new byte[1024 * 1024];
        var finished = false;
        var curPos = 0;

        while (!finished) {
            using var cancellationSource = new CancellationTokenSource();
            var left = receiveBuf.Length - curPos;
            if (left < 0) {
                // No space left in the array, enlarge the array by doubling its size.
                var temp = new byte[receiveBuf.Length * 2];
                Array.Copy(receiveBuf, temp, receiveBuf.Length);
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

        return response;
    }
}
