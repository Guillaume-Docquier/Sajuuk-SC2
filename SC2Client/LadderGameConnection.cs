using SC2APIProtocol;

namespace SC2Client;

/// <summary>
/// The ladder game connection is used to connect to an already set up ladder game against a non-computer opponent.
/// </summary>
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

    public async Task<IGame> JoinGame(Race race) {
        await _sc2Client.ConnectToGameClient(_serverAddress, _gamePort);

        _logger.Info("Joining ladder game");
        var playerId = await _sc2Client.JoinGame(RequestBuilder.RequestJoinLadderGame(race, _startPort));

        return new Game(playerId, _logger, _sc2Client);
    }
}
