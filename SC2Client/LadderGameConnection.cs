using SC2APIProtocol;

namespace SC2Client;

public class LadderGameConnection : IGameConnection {
    private readonly ILogger _logger;
    private readonly ISc2Client _sc2Client;
    private readonly string _serverAddress;
    private readonly int _gamePort;
    private readonly int _startPort;

    public LadderGameConnection(
        ILogger logger,
        ISc2Client sc2Client,
        string serverAddress,
        int gamePort,
        int startPort
    ) {
        _logger = logger;
        _sc2Client = sc2Client;
        _serverAddress = serverAddress;
        _gamePort = gamePort;
        _startPort = startPort;
    }

    public async Task<uint> JoinGame(Race race) {
        await _sc2Client.ConnectToGameClient(_serverAddress, _gamePort);

        _logger.Info("Joining ladder game");
        return await _sc2Client.JoinGame(RequestBuilder.RequestJoinLadderGame(race, _startPort));
    }
}
