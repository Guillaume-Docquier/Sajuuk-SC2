using System;
using Bot.GameSense;
using Bot.MapAnalysis.ExpandAnalysis;
using Bot.MapAnalysis.RegionAnalysis;
using SC2APIProtocol;

namespace Bot.Managers.ScoutManagement.ScoutingStrategies;

public static class ScoutingStrategyFactory {
    public static IScoutingStrategy CreateNew(
        IEnemyRaceTracker enemyRaceTracker,
        IVisibilityTracker visibilityTracker,
        IUnitsTracker unitsTracker,
        ITerrainTracker terrainTracker,
        IRegionsTracker regionsTracker
    ) {
        var enemyRace = enemyRaceTracker.EnemyRace;

        return enemyRace switch
        {
            Race.Protoss => new ProtossScoutingStrategy(visibilityTracker, unitsTracker, terrainTracker, regionsTracker),
            Race.Zerg => new ZergScoutingStrategy(visibilityTracker, unitsTracker, terrainTracker, regionsTracker),
            Race.Terran => new TerranScoutingStrategy(visibilityTracker, unitsTracker, terrainTracker, regionsTracker),
            Race.Random => new RandomScoutingStrategy(enemyRaceTracker, visibilityTracker, unitsTracker, terrainTracker, regionsTracker),
            Race.NoRace => throw new ArgumentException("Race.NoRace is an invalid ScoutingStrategy Race"),
            _ => throw new ArgumentOutOfRangeException(nameof(enemyRace), enemyRace, "Unsupported ScoutingStrategy Race")
        };
    }
}
