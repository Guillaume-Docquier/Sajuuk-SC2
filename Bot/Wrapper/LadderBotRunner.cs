using System.Threading.Tasks;
using Bot.Actions;
using Bot.Debugging.GraphicalDebugging;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapAnalysis;

namespace Bot.Wrapper;

public class LadderBotRunner : BotRunner {
    private readonly ISc2Client _sc2Client;

    private readonly IBot _bot;
    private readonly string _serverAddress;
    private readonly int _gamePort;
    private readonly int _startPort;

    // TODO GD Use composition over inheritance to share BotRunner implementation
    public LadderBotRunner(
        ISc2Client sc2Client,
        IRequestBuilder requestBuilder,
        KnowledgeBase knowledgeBase,
        IFrameClock frameClock,
        IController controller,
        IActionService actionService,
        IGraphicalDebugger graphicalDebugger,
        IUnitsTracker unitsTracker,
        IPathfinder pathfinder,
        IBot bot,
        uint stepSize,
        string serverAddress,
        int gamePort,
        int startPort
    ) : base(sc2Client, requestBuilder, knowledgeBase, frameClock, controller, actionService, graphicalDebugger, unitsTracker, pathfinder, stepSize) {
        _sc2Client = sc2Client;

        _bot = bot;
        _serverAddress = serverAddress;
        _gamePort = gamePort;
        _startPort = startPort;
    }

    public override async Task PlayGame() {
        await _sc2Client.Connect(_serverAddress, _gamePort);

        var playerId = await _sc2Client.JoinLadderGame(_bot.Race, _startPort);
        await Run(_bot, playerId);
    }
}
