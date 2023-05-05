using Bot.GameSense;
using Bot.Managers.WarManagement.States.EarlyGame;
using Bot.Managers.WarManagement.States.Finisher;
using Bot.Managers.WarManagement.States.MidGame;

namespace Bot.Managers.WarManagement.States;

public class WarManagerStateFactory : IWarManagerStateFactory {
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IWarManagerBehaviourFactory _warManagerBehaviourFactory;

    public WarManagerStateFactory(
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IWarManagerBehaviourFactory warManagerBehaviourFactory
    ) {
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _warManagerBehaviourFactory = warManagerBehaviourFactory;
    }

    public EarlyGameState CreateEarlyGameState() {
        return new EarlyGameState(this, _warManagerBehaviourFactory);
    }

    public MidGameState CreateMidGameState() {
        return new MidGameState(_unitsTracker, _terrainTracker, this, _warManagerBehaviourFactory);
    }

    public FinisherState CreateFinisherState() {
        return new FinisherState(_warManagerBehaviourFactory);
    }
}
