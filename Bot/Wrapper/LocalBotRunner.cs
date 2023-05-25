using System.Threading.Tasks;
using Bot.Actions;
using Bot.Debugging.GraphicalDebugging;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapAnalysis;
using SC2APIProtocol;

namespace Bot.Wrapper;

public class LocalBotRunner : BotRunner {
    private readonly ISc2Client _sc2Client;

    private readonly IBot _bot;
    private readonly string _mapFileName;
    private readonly Race _opponentRace;
    private readonly Difficulty _opponentDifficulty;
    private readonly bool _realTime;

    private const string ServerAddress = "127.0.0.1";
    private const int GamePort = 5678;

    // TODO GD Use composition over inheritance to share BotRunner implementation
    public LocalBotRunner(
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
        string mapFileName,
        Race opponentRace,
        Difficulty opponentDifficulty,
        uint stepSize,
        bool realTime
    ) : base(sc2Client, requestBuilder, knowledgeBase, frameClock, controller, actionService, graphicalDebugger, unitsTracker, pathfinder, stepSize) {
        _sc2Client = sc2Client;

        _bot = bot;
        _mapFileName = mapFileName;
        _opponentRace = opponentRace;
        _opponentDifficulty = opponentDifficulty;
        _realTime = realTime;
    }

    public override async Task PlayGame() {
        await _sc2Client.LaunchSc2(ServerAddress, GamePort);
        await _sc2Client.Connect(ServerAddress, GamePort);
        await _sc2Client.CreateGame(_mapFileName, _opponentRace, _opponentDifficulty, _realTime);

        var playerId = await _sc2Client.JoinLocalGame(_bot.Race);
        await Run(_bot, playerId);
    }
}
