using System.Threading.Tasks;
using SC2APIProtocol;

namespace Sajuuk.Wrapper;

public class LocalGame: IGame {
    private readonly ISc2Client _sc2Client;
    private readonly IRequestBuilder _requestBuilder;
    private readonly string _mapFileName;
    private readonly Race _opponentRace;
    private readonly Difficulty _opponentDifficulty;
    private readonly bool _realTime;

    private const string ServerAddress = "127.0.0.1";
    private const int GamePort = 5678;

    public LocalGame(
        ISc2Client sc2Client,
        IRequestBuilder requestBuilder,
        string mapFileName,
        Race opponentRace,
        Difficulty opponentDifficulty,
        bool realTime
    ) {
        _sc2Client = sc2Client;
        _requestBuilder = requestBuilder;
        _mapFileName = mapFileName;
        _opponentRace = opponentRace;
        _opponentDifficulty = opponentDifficulty;
        _realTime = realTime;
    }

    public async Task Setup() {
        await _sc2Client.LaunchSc2(ServerAddress, GamePort);
        await _sc2Client.Connect(ServerAddress, GamePort);
        await _sc2Client.CreateGame(_mapFileName, _opponentRace, _opponentDifficulty, _realTime);
    }

    public Task<uint> Join(Race race) {
        Logger.Info("Joining local game");
        return _sc2Client.JoinGame(_requestBuilder.RequestJoinLocalGame(race));
    }
}
