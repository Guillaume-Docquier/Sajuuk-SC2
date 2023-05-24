using System;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Bot.Actions;
using Bot.Debugging.GraphicalDebugging;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapAnalysis;
using Bot.Requests;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.Wrapper;

public class LadderBotRunner : IBotRunner {
    private readonly ISc2Client _sc2Client;
    private readonly IRequestService _requestService;
    private readonly IRequestBuilder _requestBuilder;
    private readonly KnowledgeBase _knowledgeBase;
    private readonly IFrameClock _frameClock;
    private readonly IController _controller;
    private readonly IActionService _actionService;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IUnitsTracker _unitsTracker;
    private readonly IPathfinder _pathfinder;

    private readonly IBot _bot;
    private readonly uint _stepSize;
    private readonly string _serverAddress;
    private readonly int _gamePort;
    private readonly int _startPort;

    // TODO GD Could be injected
    private readonly PerformanceDebugger _performanceDebugger = new PerformanceDebugger();
    private static readonly ulong DebugMemoryEvery = TimeUtils.SecsToFrames(5);

    public LadderBotRunner(
        ISc2Client sc2Client,
        IRequestService requestService,
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
    ) {
        _sc2Client = sc2Client;
        _requestService = requestService;
        _requestBuilder = requestBuilder;
        _knowledgeBase = knowledgeBase;
        _frameClock = frameClock;
        _controller = controller;
        _actionService = actionService;
        _graphicalDebugger = graphicalDebugger;
        _unitsTracker = unitsTracker;
        _pathfinder = pathfinder;

        _bot = bot;
        _stepSize = stepSize;
        _serverAddress = serverAddress;
        _gamePort = gamePort;
        _startPort = startPort;
    }

    public async Task PlayGame() {
        await ConnectToSc2Instance(_serverAddress, _gamePort);

        var playerId = await JoinGame(_bot.Race, _startPort);
        await Run(_bot, playerId);
    }

    // TODO GD That retry logic should probably go into the proxy
    private async Task ConnectToSc2Instance(string serverAddress, int gamePort) {
        const int timeout = 60;
        for (var i = 0; i < timeout * 2; i++) {
            try {
                await _sc2Client.Connect(serverAddress, gamePort);
                Logger.Info("--> Connected");

                return;
            }
            catch (WebSocketException) {
                Logger.Warning("Failed. Retrying...");
            }

            Thread.Sleep(500);
        }

        Logger.Error("Unable to connect to SC2 after {0} seconds.", timeout);
        throw new Exception("Unable to make a connection.");
    }

    private async Task<uint> JoinGame(Race race, int startPort) {
        var joinGameResponse = await _requestService.SendRequest(_requestBuilder.RequestJoinLadderGame(race, startPort), logErrors: true);

        // TODO GD This might be broken now, used to be ResponseJoinGame.Types.Error.Unset (0) but it doesn't exist anymore
        if (joinGameResponse.JoinGame.Error != ResponseJoinGame.Types.Error.MissingParticipation) {
            Logger.Error($"JoinGame error: {joinGameResponse.JoinGame.Error.ToString()}");
            if (!string.IsNullOrEmpty(joinGameResponse.JoinGame.ErrorDetails)) {
                Logger.Error(joinGameResponse.JoinGame.ErrorDetails);
            }
        }

        return joinGameResponse.JoinGame.PlayerId;
    }

    private async Task Run(IBot bot, uint playerId, bool runDataAnalyzersOnly = false) {
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
        var dataResponse = await _requestService.SendRequest(dataRequest);
        _knowledgeBase.Data = dataResponse.Data;

        while (true) {
            // _frameClock.CurrentFrame is uint.MaxValue until we request frame 0
            var nextFrame = _frameClock.CurrentFrame == uint.MaxValue ? 0 : _frameClock.CurrentFrame + _stepSize;
            var observationResponse = await _requestService.SendRequest(_requestBuilder.RequestObservation(nextFrame));

            if (observationResponse.Status is Status.Quit) {
                Logger.Info("Game was terminated.");
                break;
            }

            var observation = observationResponse.Observation;
            if (observationResponse.Status is Status.Ended) {
                _performanceDebugger.LogAveragePerformance();

                foreach (var result in observation.PlayerResult) {
                    if (result.PlayerId == playerId) {
                        Logger.Info("Result: {0}", result.Result);
                    }
                }

                break;
            }

            await RunBot(bot, observation);

            if (observation.Observation.GameLoop % DebugMemoryEvery == 0) {
                PrintMemoryInfo();
            }

            await _requestService.SendRequest(_requestBuilder.RequestStep(_stepSize));
        }
    }

    private async Task RunBot(IBot bot, ResponseObservation observation) {
        var gameInfoResponse = await _requestService.SendRequest(_requestBuilder.RequestGameInfo());

        _performanceDebugger.FrameStopwatch.Start();

        _performanceDebugger.ControllerStopwatch.Start();
        _controller.NewFrame(gameInfoResponse.GameInfo, observation);
        _performanceDebugger.ControllerStopwatch.Stop();

        _performanceDebugger.BotStopwatch.Start();
        await bot.OnFrame();
        _performanceDebugger.BotStopwatch.Stop();

        _performanceDebugger.ActionsStopwatch.Start();
        var actions = _actionService.GetActions().ToList();

        if (actions.Count > 0) {
            var response = await _requestService.SendRequest(_requestBuilder.RequestAction(actions));

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
            await _requestService.SendRequest(request);
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
                _pathfinder.CellPathsMemory.Values.SelectMany(destinations => destinations.Values).Sum(path => path.Count));
            Logger.Performance("==== Memory Debug End ====");
        }
    }
}
