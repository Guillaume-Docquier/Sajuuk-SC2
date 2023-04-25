using System;
using Bot.GameSense;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.Managers.ScoutManagement.ScoutingStrategies;

public static class ScoutingStrategyFactory {
    public static IScoutingStrategy CreateNew(
        IEnemyRaceTracker enemyRaceTracker,
        IVisibilityTracker visibilityTracker,
        IUnitsTracker unitsTracker,
        IMapAnalyzer mapAnalyzer,
        IExpandAnalyzer expandAnalyzer
    ) {
        var enemyRace = enemyRaceTracker.EnemyRace;

        return enemyRace switch
        {
            Race.Protoss => new ProtossScoutingStrategy(visibilityTracker, unitsTracker, mapAnalyzer, expandAnalyzer),
            Race.Zerg => new ZergScoutingStrategy(visibilityTracker, unitsTracker, mapAnalyzer, expandAnalyzer),
            Race.Terran => new TerranScoutingStrategy(visibilityTracker, unitsTracker, mapAnalyzer, expandAnalyzer),
            Race.Random => new RandomScoutingStrategy(enemyRaceTracker, visibilityTracker, unitsTracker, mapAnalyzer, expandAnalyzer),
            Race.NoRace => throw new ArgumentException("Race.NoRace is an invalid ScoutingStrategy Race"),
            _ => throw new ArgumentOutOfRangeException(nameof(enemyRace), enemyRace, "Unsupported ScoutingStrategy Race")
        };
    }
}
