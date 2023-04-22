using Bot.Tagging;
using SC2APIProtocol;

namespace Bot.Tests.Mocks;

public class DummyTaggingService : ITaggingService {
    public bool HasTagged(Tag tag) {
        return true;
    }

    public void TagEarlyAttack() {}
    public void TagTerranFinisher() {}
    public void TagBuildDone(uint supply, float collectedMinerals, float collectedVespene) {}
    public void TagEnemyStrategy(string enemyStrategy) {}
    public void TagVersion(string version) {}
    public void TagMinerals(float collectedMinerals) {}
    public void TagEnemyRace(Race actualEnemyRace) {}
}
