using SC2APIProtocol;

namespace SC2Client;

public class LocalGameConnection : IGameConnection {
    private readonly ILogger _logger;
    private readonly ISc2Client _sc2Client;
    private readonly string _mapFileName;
    private readonly Race _opponentRace;
    private readonly Difficulty _opponentDifficulty;
    private readonly bool _realTime;

    private const string ServerAddress = "127.0.0.1";
    private const int GamePort = 5678;

    public LocalGameConnection(
        ILogger logger,
        ISc2Client sc2Client,
        string mapFileName,
        Race opponentRace,
        Difficulty opponentDifficulty,
        bool realTime
    ) {
        _logger = logger;
        _sc2Client = sc2Client;
        _mapFileName = mapFileName;
        _opponentRace = opponentRace;
        _opponentDifficulty = opponentDifficulty;
        _realTime = realTime;
    }

    public async Task<uint> JoinGame(Race race) {
        _sc2Client.LaunchSc2(ServerAddress, GamePort);
        await _sc2Client.ConnectToGameClient(ServerAddress, GamePort);
        await _sc2Client.CreateGame(_mapFileName, _opponentRace, _opponentDifficulty, _realTime);

        _logger.Info("Joining local game");
        return await _sc2Client.JoinGame(RequestBuilder.RequestJoinLocalGame(race));
    }
}
