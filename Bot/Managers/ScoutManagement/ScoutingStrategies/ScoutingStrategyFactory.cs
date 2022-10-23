using System;
using SC2APIProtocol;

namespace Bot.Managers.ScoutManagement.ScoutingStrategies;

public static class ScoutingStrategyFactory {
    public static IScoutingStrategy CreateNew(Race enemyRace) {
        return enemyRace switch
        {
            Race.Protoss => new ProtossScoutingStrategy(),
            Race.Zerg => new ZergScoutingStrategy(),
            Race.Terran => new TerranScoutingStrategy(),
            Race.Random => new RandomScoutingStrategy(),
            Race.NoRace => throw new ArgumentException("Race.NoRace is an invalid ScoutingStrategy Race"),
            _ => throw new ArgumentOutOfRangeException(nameof(enemyRace), enemyRace, "Unsupported ScoutingStrategy Race")
        };
    }
}
