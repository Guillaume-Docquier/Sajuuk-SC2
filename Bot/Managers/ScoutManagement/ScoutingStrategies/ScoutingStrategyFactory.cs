using System;
using Bot.GameSense;
using SC2APIProtocol;

namespace Bot.Managers.ScoutManagement.ScoutingStrategies;

public static class ScoutingStrategyFactory {
    public static IScoutingStrategy CreateNew(IEnemyRaceTracker enemyRaceTracker, IVisibilityTracker visibilityTracker, IUnitsTracker unitsTracker) {
        var enemyRace = enemyRaceTracker.EnemyRace;

        return enemyRace switch
        {
            Race.Protoss => new ProtossScoutingStrategy(visibilityTracker, unitsTracker),
            Race.Zerg => new ZergScoutingStrategy(visibilityTracker, unitsTracker),
            Race.Terran => new TerranScoutingStrategy(visibilityTracker, unitsTracker),
            Race.Random => new RandomScoutingStrategy(enemyRaceTracker, visibilityTracker, unitsTracker),
            Race.NoRace => throw new ArgumentException("Race.NoRace is an invalid ScoutingStrategy Race"),
            _ => throw new ArgumentOutOfRangeException(nameof(enemyRace), enemyRace, "Unsupported ScoutingStrategy Race")
        };
    }
}
