using Bot.GameSense;
using Bot.Managers.WarManagement.States.EarlyGame;
using Bot.Managers.WarManagement.States.Finisher;
using Bot.Managers.WarManagement.States.MidGame;

namespace Bot.Managers.WarManagement.States;

public class WarManagerStateFactory : IWarManagerStateFactory {
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IWarManagerBehaviourFactory _warManagerBehaviourFactory;
    private readonly IFrameClock _frameClock;
    private readonly IUnitEvaluator _unitEvaluator;

    public WarManagerStateFactory(
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IWarManagerBehaviourFactory warManagerBehaviourFactory,
        IFrameClock frameClock,
        IUnitEvaluator unitEvaluator
    ) {
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _warManagerBehaviourFactory = warManagerBehaviourFactory;
        _frameClock = frameClock;
        _unitEvaluator = unitEvaluator;
    }

    public EarlyGameState CreateEarlyGameState() {
        return new EarlyGameState(this, _warManagerBehaviourFactory, _frameClock);
    }

    public MidGameState CreateMidGameState() {
        return new MidGameState(_unitsTracker, _terrainTracker, this, _warManagerBehaviourFactory, _unitEvaluator);
    }

    public FinisherState CreateFinisherState() {
        return new FinisherState(_warManagerBehaviourFactory);
    }
}
