using SC2APIProtocol;

namespace Sajuuk.Managers.ScoutManagement.ScoutingStrategies;

public interface IScoutingStrategyFactory {
    public IScoutingStrategy CreateNew(Race enemyRace);
}
