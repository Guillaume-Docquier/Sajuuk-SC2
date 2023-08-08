using System.Threading.Tasks;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.GameData;
using Sajuuk.Wrapper;
using SC2APIProtocol;

namespace Sajuuk.MapAnalysis;

public class MapAnalysisBotRunner : IBotRunner {
    private readonly IGame _game;
    private readonly ISc2Client _sc2Client;
    private readonly KnowledgeBase _knowledgeBase;
    private readonly IRequestBuilder _requestBuilder;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IController _controller;

    private const uint StepSize = 1;

    public MapAnalysisBotRunner(
        IGame game,
        ISc2Client sc2Client,
        KnowledgeBase knowledgeBase,
        IRequestBuilder requestBuilder,
        IGraphicalDebugger graphicalDebugger,
        IController controller
    ) {
        _game = game;
        _sc2Client = sc2Client;
        _knowledgeBase = knowledgeBase;
        _requestBuilder = requestBuilder;
        _graphicalDebugger = graphicalDebugger;
        _controller = controller;
    }

    public async Task RunBot(IBot bot) {
        await _game.Setup();
        await _game.Join(bot.Race);
        await InitializeKnowledgeBase();

        await RunGameLoops(bot);
    }

    /// <summary>
    /// Initializes the knowledge base by requesting the data from the API.
    /// </summary>
    private async Task InitializeKnowledgeBase() {
        var dataRequest = new Request
        {
            Data = new RequestData
            {
                UnitTypeId = true,
                AbilityId = true,
                BuffId = true,
                EffectId = true,
                UpgradeId = true,
            }
        };
        var dataResponse = await _sc2Client.SendRequest(dataRequest);
        _knowledgeBase.Data = dataResponse.Data;
    }

    /// <summary>
    /// Runs game loops with the bot until the game is over.
    /// </summary>
    /// <param name="bot">The bot that plays.</param>
    private async Task RunGameLoops(IBot bot) {
        uint nextFrame = 0;

        while (true) {
            var observationResponse = await _sc2Client.SendRequest(_requestBuilder.RequestObservation(nextFrame));

            if (observationResponse.Status is Status.Quit or Status.Ended) {
                Logger.Info("Game was terminated.");
                break;
            }

            await RunBotFrame(bot, observationResponse.Observation);

            await _sc2Client.SendRequest(_requestBuilder.RequestStep(StepSize));
            nextFrame = observationResponse.Observation.Observation.GameLoop + StepSize;
        }
    }

    /// <summary>
    /// Runs the bot and anything bot related.
    /// </summary>
    /// <param name="bot">The bot that plays.</param>
    /// <param name="observation">The game loop observation.</param>
    private async Task RunBotFrame(IBot bot, ResponseObservation observation) {
        var gameInfoResponse = await _sc2Client.SendRequest(_requestBuilder.RequestGameInfo());
        _controller.NewFrame(gameInfoResponse.GameInfo, observation);
        await bot.OnFrame();

        var graphicalDebuggingRequest = _graphicalDebugger.GetDebugRequest();
        if (graphicalDebuggingRequest != null) {
            await _sc2Client.SendRequest(graphicalDebuggingRequest);
        }
    }
}
