using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Sajuuk.Actions;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.ExtensionMethods;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.MapAnalysis;
using Sajuuk.Utils;
using SC2APIProtocol;

namespace Sajuuk.Wrapper;

public class BotRunner : IBotRunner {
    private readonly ISc2Client _sc2Client;
    private readonly IGame _game;
    private readonly IRequestBuilder _requestBuilder;
    private readonly KnowledgeBase _knowledgeBase;
    private readonly IFrameClock _frameClock;
    private readonly IController _controller;
    private readonly IActionService _actionService;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IUnitsTracker _unitsTracker;
    private readonly IPathfinder _pathfinder;
    private readonly uint _stepSize;

    // TODO GD Could be injected
    private readonly PerformanceDebugger _performanceDebugger = new PerformanceDebugger();
    private static readonly ulong DebugMemoryEvery = TimeUtils.SecsToFrames(5);

    public BotRunner(
        ISc2Client sc2Client,
        IGame game,
        IRequestBuilder requestBuilder,
        KnowledgeBase knowledgeBase,
        IFrameClock frameClock,
        IController controller,
        IActionService actionService,
        IGraphicalDebugger graphicalDebugger,
        IUnitsTracker unitsTracker,
        IPathfinder pathfinder,
        uint stepSize
    ) {
        _sc2Client = sc2Client;
        _game = game;
        _requestBuilder = requestBuilder;
        _knowledgeBase = knowledgeBase;
        _frameClock = frameClock;
        _controller = controller;
        _actionService = actionService;
        _graphicalDebugger = graphicalDebugger;
        _unitsTracker = unitsTracker;
        _pathfinder = pathfinder;
        _stepSize = stepSize;
    }

    public async Task RunBot(IBot bot) {
        await _game.Setup();
        var playerId = await _game.Join(bot.Race);
        await InitializeKnowledgeBase();

        await RunGameLoops(bot, playerId);
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
    /// <param name="playerId">The player id of the bot.</param>
    private async Task RunGameLoops(IBot bot, uint playerId) {
        while (true) {
            // _frameClock.CurrentFrame is uint.MaxValue until we request frame 0
            var nextFrame = _frameClock.CurrentFrame == uint.MaxValue ? 0 : _frameClock.CurrentFrame + _stepSize;
            var observationResponse = await _sc2Client.SendRequest(_requestBuilder.RequestObservation(nextFrame));

            if (HandleGameTermination(observationResponse, playerId)) {
                _performanceDebugger.LogAveragePerformance();
                break;
            }

            var observation = observationResponse.Observation;

            await RunBotFrame(bot, observation);

            if (observation.Observation.GameLoop % DebugMemoryEvery == 0) {
                PrintMemoryInfo();
            }

            await _sc2Client.SendRequest(_requestBuilder.RequestStep(_stepSize));
        }
    }

    /// <summary>
    /// Handles the case where the game was terminated.
    /// </summary>
    /// <param name="observationResponse">The game observation response.</param>
    /// <param name="playerId">The player id of the bot.</param>
    /// <returns>True if the game has ended, false otherwise.</returns>
    private static bool HandleGameTermination(Response observationResponse, uint playerId) {
        switch (observationResponse.Status) {
            case Status.Quit:
                Logger.Info("Game was terminated.");
                return true;
            case Status.Ended:
                var gameResult = observationResponse.Observation.PlayerResult.First(result => result.PlayerId == playerId);
                Logger.Info($"Game is over, result: {gameResult.Result}");
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Runs the bot and anything bot related and mesures their CPU time performance.
    /// </summary>
    /// <param name="bot">The bot that plays.</param>
    /// <param name="observation">The game loop observation.</param>
    private async Task RunBotFrame(IBot bot, ResponseObservation observation) {
        var gameInfoResponse = await _sc2Client.SendRequest(_requestBuilder.RequestGameInfo());

        _performanceDebugger.FrameStopwatch.Start();

        _performanceDebugger.ControllerStopwatch.Start();
        _controller.NewFrame(gameInfoResponse.GameInfo, observation);
        _performanceDebugger.ControllerStopwatch.Stop();

        _performanceDebugger.BotStopwatch.Start();
        await bot.OnFrame();
        foreach (var unit in _unitsTracker.OwnedUnits)
        {
            foreach (var action in unit.Actions)
            {
                var unitCommand = new ActionRawUnitCommand()
                {
                    QueueCommand = action.QueueComannd,
                    AbilityId = (int)action.AbilityId,
                };
                if (action.TargetPosition != null)
                {
                    unitCommand.TargetWorldSpacePos = action.TargetPosition.Value.ToPoint2D();
                }
                if (action.TargetUnit != null)
                {
                    unitCommand.TargetUnitTag = action.TargetUnit.Value;
                }
                unitCommand.UnitTags.Add(unit.Tag);
                _actionService.AddAction(new Action()
                {
                    ActionRaw = new ActionRaw()
                    {
                        UnitCommand = unitCommand,
                    }
                });
            }
            unit.Actions.Clear();
        }
        _performanceDebugger.BotStopwatch.Stop();

        _performanceDebugger.ActionsStopwatch.Start();
        var actions = _actionService.GetActions().ToList();

        if (actions.Count > 0) {
            var response = await _sc2Client.SendRequest(_requestBuilder.RequestAction(actions));

            var unsuccessfulActions = actions
                .Zip(response.Action.Result, (action, result) => (action, result))
                .Where(action => action.result != ActionResult.Success)
                .Select(action => $"({_knowledgeBase.GetAbilityData(action.action.ActionRaw.UnitCommand.AbilityId).FriendlyName}, {action.result})")
                .ToList();

            if (unsuccessfulActions.Count > 0) {
                Logger.Warning("Unsuccessful actions: [{0}]", string.Join("; ", unsuccessfulActions));
            }
        }
        _performanceDebugger.ActionsStopwatch.Stop();

        _performanceDebugger.DebuggerStopwatch.Start();
        var request = _graphicalDebugger.GetDebugRequest();
        if (request != null) {
            await _sc2Client.SendRequest(request);
        }
        _performanceDebugger.DebuggerStopwatch.Stop();

        _performanceDebugger.FrameStopwatch.Stop();

        if (_performanceDebugger.FrameStopwatch.ElapsedMilliseconds > 10) {
            _performanceDebugger.LogTimers(actions.Count);
        }

        _performanceDebugger.CompileData();
    }

    private void PrintMemoryInfo() {
        var memoryUsedMb = Process.GetCurrentProcess().WorkingSet64 * 1e-6;
        if (memoryUsedMb > 200) {
            Logger.Performance("==== Memory Debug Start ====");
            Logger.Performance("Memory used: {0} MB", memoryUsedMb.ToString("0.00"));
            Logger.Performance("Units: {0} owned, {1} neutral, {2} enemy", _unitsTracker.OwnedUnits.Count, _unitsTracker.NeutralUnits.Count, _unitsTracker.EnemyUnits.Count);
            Logger.Performance(
                "Pathfinding cache: {0} paths, {1} tiles",
                _pathfinder.CellPathsMemory.Values.Sum(destinations => destinations.Keys.Count),
                _pathfinder.CellPathsMemory.Values.SelectMany(destinations => destinations.Values).Sum(path => path.Count)
            );
            Logger.Performance("==== Memory Debug End ====");
        }
    }
}
