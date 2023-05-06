using System;
using Bot.GameSense;
using Bot.Managers.ScoutManagement.ScoutingTasks;
using SC2APIProtocol;

namespace Bot.Managers.ScoutManagement.ScoutingStrategies;

public class ScoutingStrategyFactory : IScoutingStrategyFactory {
    private readonly IRegionsTracker _regionsTracker;
    private readonly IScoutingTaskFactory _scoutingTaskFactory;
    private readonly IUnitsTracker _unitsTracker;
    private readonly IEnemyRaceTracker _enemyRaceTracker;

    public ScoutingStrategyFactory(
        IRegionsTracker regionsTracker,
        IScoutingTaskFactory scoutingTaskFactory,
        IUnitsTracker unitsTracker,
        IEnemyRaceTracker enemyRaceTracker
    ) {
        _regionsTracker = regionsTracker;
        _scoutingTaskFactory = scoutingTaskFactory;
        _unitsTracker = unitsTracker;
        _enemyRaceTracker = enemyRaceTracker;
    }

    public IScoutingStrategy CreateNew(Race enemyRace) {
        return enemyRace switch
        {
            Race.Protoss => new ProtossScoutingStrategy(_regionsTracker, _scoutingTaskFactory),
            Race.Zerg => new ZergScoutingStrategy(_unitsTracker, _regionsTracker, _scoutingTaskFactory),
            Race.Terran => new TerranScoutingStrategy(_regionsTracker, _scoutingTaskFactory),
            Race.Random => new RandomScoutingStrategy(_enemyRaceTracker, _regionsTracker, _scoutingTaskFactory, this),
            Race.NoRace => throw new ArgumentException("Race.NoRace is an invalid ScoutingStrategy Race"),
            _ => throw new ArgumentOutOfRangeException(nameof(enemyRace), enemyRace, "Unsupported ScoutingStrategy Race")
        };
    }
}
