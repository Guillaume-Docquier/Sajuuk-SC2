using SC2APIProtocol;
using SC2Client.GameData;
using SC2Client.GameState;

namespace SC2Client;

// TODO GD GameFactory?
public class Game : IGame {
    private readonly uint _playerId;
    private readonly ILogger _logger;
    private readonly ISc2Client _sc2Client;
    private readonly Terrain _terrain;

    private Game(
        ResponseGameInfo gameInfo,
        Status gameStatus,
        ResponseObservation observation,
        ResponseData data,
        uint playerId,
        ILogger logger,
        ISc2Client sc2Client
    ) {
        _playerId = playerId;
        _logger = logger;
        _sc2Client = sc2Client;
        KnowledgeBase = new KnowledgeBase(data);
        _terrain = new Terrain(new FootprintCalculator(_logger), gameInfo);

        UpdateState(gameStatus, observation);
    }

    public static async Task<Game> Create(
        uint playerId,
        ILogger logger,
        ISc2Client sc2Client
    ) {
        // TODO GD I think we can merge these 3 requests into 1
        var gameInfoResponse = await sc2Client.SendRequest(RequestBuilder.RequestGameInfo());
        var observationResponse = await sc2Client.SendRequest(RequestBuilder.RequestObservation(0));
        var dataResponse = await sc2Client.SendRequest(RequestBuilder.RequestData());

        return new Game(
            gameInfoResponse.GameInfo,
            observationResponse.Status,
            observationResponse.Observation,
            dataResponse.Data,
            playerId,
            logger,
            sc2Client
        );
    }

    public uint CurrentFrame { get; private set; } = 0;
    public Result GameResult { get; private set; } = Result.Undecided;
    public KnowledgeBase KnowledgeBase { get; init;  }

    public ITerrain Terrain => _terrain;

    public async Task Step(uint stepSize) {
        // TODO Send actions

        await _sc2Client.SendRequest(RequestBuilder.RequestStep(stepSize));

        var observationResponse = await _sc2Client.SendRequest(RequestBuilder.RequestObservation(CurrentFrame + stepSize));

        UpdateState(observationResponse.Status, observationResponse.Observation);
    }

    /// <summary>
    /// Updates the game state.
    /// </summary>
    /// <param name="gameStatus">The status of the game.</param>
    /// <param name="observation">The current game state observation.</param>
    private void UpdateState(Status gameStatus, ResponseObservation observation) {
        CurrentFrame = observation.Observation.GameLoop;
        GameResult = GetGameResult(gameStatus, observation);

        _terrain.Update(observation);
    }

    public void Surrender() {
        _logger.Info("Surrendering the game...");
        _sc2Client.LeaveCurrentGame().Wait();
    }

    /// <summary>
    /// Gets the game result from the response.
    /// </summary>
    /// <param name="gameStatus">The status of the game.</param>
    /// <param name="observation">The current game state observation.</param>
    /// <returns>The current game result.</returns>
    private Result GetGameResult(Status gameStatus, ResponseObservation observation) {
        switch (gameStatus) {
            case Status.Quit:
                _logger.Warning("Game was terminated.");
                return Result.Defeat;
            case Status.Ended:
                return observation.PlayerResult.First(result => result.PlayerId == _playerId).Result;
            default:
                return Result.Undecided;
        }
    }
}
