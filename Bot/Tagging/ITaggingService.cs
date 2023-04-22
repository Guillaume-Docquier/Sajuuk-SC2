using SC2APIProtocol;

namespace Bot.Tagging;

public interface ITaggingService {
    public bool HasTagged(Tag tag);

    public void TagEarlyAttack();
    public void TagTerranFinisher();
    public void TagBuildDone(uint supply, float collectedMinerals, float collectedVespene);
    public void TagEnemyStrategy(string enemyStrategy);
    public void TagVersion(string version);
    public void TagMinerals(float collectedMinerals);
    public void TagEnemyRace(Race actualEnemyRace);
}
