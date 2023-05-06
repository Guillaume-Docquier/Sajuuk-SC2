using SC2APIProtocol;

namespace Bot.Managers.ScoutManagement.ScoutingStrategies;

public interface IScoutingStrategyFactory {
    public IScoutingStrategy CreateNew(Race enemyRace);
}
