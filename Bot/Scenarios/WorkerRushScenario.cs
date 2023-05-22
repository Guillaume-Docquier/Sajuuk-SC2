﻿using System.Threading.Tasks;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapAnalysis.ExpandAnalysis;
using Bot.Utils;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.Scenarios;

// Simulates a worker rush by smallBly https://aiarena.net/bots/338/
public class WorkerRushScenario: IScenario {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly IFrameClock _frameClock;
    private readonly IController _controller;
    private readonly IRequestBuilder _requestBuilder;

    private const int DefaultTimingInSeconds = 50;

    private readonly int _timingInSeconds;
    private bool _isScenarioDone = false;

    public WorkerRushScenario(
        ITerrainTracker terrainTracker,
        IRegionsTracker regionsTracker,
        IFrameClock frameClock,
        IController controller,
        IRequestBuilder requestBuilder,
        int timingInSeconds = DefaultTimingInSeconds
    ) {
        _terrainTracker = terrainTracker;
        _regionsTracker = regionsTracker;
        _frameClock = frameClock;
        _controller = controller;
        _requestBuilder = requestBuilder;

        _timingInSeconds = timingInSeconds;
    }

    public async Task OnFrame() {
        if (_isScenarioDone) {
            return;
        }

        if (_frameClock.CurrentFrame >= TimeUtils.SecsToFrames(_timingInSeconds)) {
            var mainPosition = _regionsTracker.GetExpand(Alliance.Self, ExpandType.Main).Position;

            Logger.Debug("Spawning 12 zerglings {0} units away from main", 0);

            // Spawned drones wouldn't be aggressive so we spawn zerglings instead
            await Program.GameConnection.SendRequest(_requestBuilder.DebugCreateUnit(Owner.Enemy, Units.Zergling, 12, _terrainTracker.WithWorldHeight(mainPosition)));
            _controller.SetRealTime("Worker rush started");

            _isScenarioDone = true;
        }
    }
}
