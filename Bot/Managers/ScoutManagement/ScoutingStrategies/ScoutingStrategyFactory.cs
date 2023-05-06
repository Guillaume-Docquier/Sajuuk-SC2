using System;
using Bot.Debugging.GraphicalDebugging;
using Bot.GameSense;
using Bot.Managers.ScoutManagement.ScoutingTasks;
using SC2APIProtocol;

namespace Bot.Managers.ScoutManagement.ScoutingStrategies;

public static class ScoutingStrategyFactory {
    public static IScoutingStrategy CreateNew(
        IEnemyRaceTracker enemyRaceTracker,
        IVisibilityTracker visibilityTracker,
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IRegionsTracker regionsTracker,
        IGraphicalDebugger graphicalDebugger,
        IScoutingTaskFactory scoutingTaskFactory
    ) {
        var enemyRace = enemyRaceTracker.EnemyRace;

        return enemyRace switch
        {
            Race.Protoss => new ProtossScoutingStrategy(regionsTracker, scoutingTaskFactory),
            Race.Zerg => new ZergScoutingStrategy(unitsTracker, regionsTracker, scoutingTaskFactory),
            Race.Terran => new TerranScoutingStrategy(regionsTracker, scoutingTaskFactory),
            Race.Random => new RandomScoutingStrategy(enemyRaceTracker, visibilityTracker, unitsTracker, terrainTracker, regionsTracker, graphicalDebugger, scoutingTaskFactory),
            Race.NoRace => throw new ArgumentException("Race.NoRace is an invalid ScoutingStrategy Race"),
            _ => throw new ArgumentOutOfRangeException(nameof(enemyRace), enemyRace, "Unsupported ScoutingStrategy Race")
        };
    }
}
