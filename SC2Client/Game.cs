using SC2APIProtocol;
using SC2Client.GameData;
using SC2Client.State;

namespace SC2Client;

public class Game : IGame {
    private readonly ILogger _logger;
    private readonly ISc2Client _sc2Client;
    private readonly GameState _state;

    public KnowledgeBase KnowledgeBase { get; }
    public IGameState State => _state;
    public bool IsOver => State.Result != Result.Undecided;

    private Game(
        ILogger logger,
        ISc2Client sc2Client,
        uint playerId,
        ResponseData data,
        ResponseGameInfo gameInfo,
        Status gameStatus,
        ResponseObservation observation
    ) {
        _logger = logger;
        _sc2Client = sc2Client;
        KnowledgeBase = new KnowledgeBase(data);
        _state = new GameState(logger, playerId, KnowledgeBase, gameInfo, gameStatus, observation);
    }

    public static async Task<Game> Create(
        uint playerId,
        ILogger logger,
        ISc2Client sc2Client
    ) {
        // TODO GD I think we can merge these 3 requests into 1
        var dataResponse = await sc2Client.SendRequest(RequestBuilder.RequestData());
        var gameInfoResponse = await sc2Client.SendRequest(RequestBuilder.RequestGameInfo());
        var observationResponse = await sc2Client.SendRequest(RequestBuilder.RequestObservation(0));

        return new Game(
            logger,
            sc2Client,
            playerId,
            dataResponse.Data,
            gameInfoResponse.GameInfo,
            observationResponse.Status,
            observationResponse.Observation
        );
    }

    public async Task Step(uint stepSize, List<SC2APIProtocol.Action> actions) {
        if (actions.Count > 0) {
            var response = await _sc2Client.SendRequest(RequestBuilder.RequestAction(actions));

            // TODO GD Return this
            var unsuccessfulActions = actions
                .Zip(response.Action.Result, (action, result) => (action, result))
                .Where(action => action.result != ActionResult.Success)
                .Select(action => $"({KnowledgeBase.GetAbilityData(action.action.ActionRaw.UnitCommand.AbilityId).FriendlyName}, {action.result})")
                .ToList();

            if (unsuccessfulActions.Count > 0) {
                _logger.Warning($"Unsuccessful actions: [{string.Join("; ", unsuccessfulActions)}]");
            }
        }

        await _sc2Client.SendRequest(RequestBuilder.RequestStep(stepSize));

        var observationResponse = await _sc2Client.SendRequest(RequestBuilder.RequestObservation(State.CurrentFrame + stepSize));

        _state.Update(observationResponse.Status, observationResponse.Observation);
    }

    public void Quit() {
        _logger.Info("Quitting the game...");
        _sc2Client.LeaveCurrentGame().Wait();
    }
}
