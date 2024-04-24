using SC2APIProtocol;

namespace SC2Client;

// TODO GD GameFactory?
public class Game : IGame {
    private readonly uint _playerId;
    private readonly ILogger _logger;
    private readonly ISc2Client _sc2Client;

    public Game(
        uint playerId,
        ILogger logger,
        ISc2Client sc2Client
    ) {
        _playerId = playerId;
        _logger = logger;
        _sc2Client = sc2Client;
    }

    public uint CurrentFrame { get; private set; } = 0;
    public Result GameResult { get; private set; } = Result.Undecided;

    public async Task Step(uint stepSize) {
        // TODO Send actions

        if (stepSize > 0) {
            await _sc2Client.SendRequest(RequestBuilder.RequestStep(stepSize));
        }

        var observationResponse = await _sc2Client.SendRequest(RequestBuilder.RequestObservation(CurrentFrame + stepSize));

        CurrentFrame = observationResponse.Observation.Observation.GameLoop;
        GameResult = GetGameResult(observationResponse);
    }

    public void Surrender() {
        _logger.Info("Surrendering the game...");
        _sc2Client.LeaveCurrentGame().Wait();
    }

    /// <summary>
    /// Gets the game result from the response.
    /// </summary>
    /// <param name="observationResponse">The game observation response.</param>
    /// <returns>The current game result.</returns>
    private Result GetGameResult(Response observationResponse) {
        switch (observationResponse.Status) {
            case Status.Quit:
                _logger.Warning("Game was terminated.");
                return Result.Defeat;
            case Status.Ended:
                return observationResponse.Observation.PlayerResult.First(result => result.PlayerId == _playerId).Result;
            default:
                return Result.Undecided;
        }
    }
}
