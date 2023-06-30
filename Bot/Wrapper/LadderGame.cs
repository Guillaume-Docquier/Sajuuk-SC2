using System.Threading.Tasks;
using SC2APIProtocol;

namespace Bot.Wrapper;

public class LadderGame : IGame {
    private readonly ISc2Client _sc2Client;
    private readonly IRequestBuilder _requestBuilder;
    private readonly string _serverAddress;
    private readonly int _gamePort;
    private readonly int _startPort;

    public LadderGame(
        ISc2Client sc2Client,
        IRequestBuilder requestBuilder,
        string serverAddress,
        int gamePort,
        int startPort
    ) {
        _sc2Client = sc2Client;
        _requestBuilder = requestBuilder;
        _serverAddress = serverAddress;
        _gamePort = gamePort;
        _startPort = startPort;
    }

    public async Task Setup() {
        await _sc2Client.Connect(_serverAddress, _gamePort);
    }

    public Task<uint> Join(Race race) {
        Logger.Info("Joining ladder game");
        return _sc2Client.JoinGame(_requestBuilder.RequestJoinLadderGame(race, _startPort));
    }
}
