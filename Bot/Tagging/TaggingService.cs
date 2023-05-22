using System.Collections.Generic;
using Bot.Utils;
using SC2APIProtocol;

namespace Bot.Tagging;

public class TaggingService : ITaggingService {
    private readonly IFrameClock _frameClock;
    private readonly IChatService _chatService;

    private const int MaximumTagLength = 32;

    private readonly HashSet<Tag> _tagsSent = new HashSet<Tag>();

    public TaggingService(
        IFrameClock frameClock,
        IChatService chatService
    ) {
        _frameClock = frameClock;
        _chatService = chatService;
    }

    public bool HasTagged(Tag tag) {
        return _tagsSent.Contains(tag);
    }

    public void TagEarlyAttack() {
        const Tag tag = Tag.EarlyAttack;
        var gameTimeString = TimeUtils.GetGameTimeString(_frameClock.CurrentFrame);

        TagGame(tag, $"{tag}_{gameTimeString}");
    }

    public void TagTerranFinisher() {
        const Tag tag = Tag.TerranFinisher;
        var gameTimeString = TimeUtils.GetGameTimeString(_frameClock.CurrentFrame);

        TagGame(tag, $"{tag}_{gameTimeString}");
    }

    public void TagBuildDone(uint supply, float collectedMinerals, float collectedVespene) {
        const Tag tag = Tag.BuildDone;
        var gameTimeString = TimeUtils.GetGameTimeString(_frameClock.CurrentFrame);

        TagGame(tag, $"{tag}_{gameTimeString}_S{supply}_M{collectedMinerals}_V{collectedVespene}");
    }

    public void TagEnemyStrategy(string enemyStrategy) {
        const Tag tag = Tag.BuildDone;
        var gameTimeString = TimeUtils.GetGameTimeString(_frameClock.CurrentFrame);

        // We print "EnemyStrategy" as "Enemy" to have more space for the enemy strategy name, otherwise it gets truncated
        TagGame(tag, $"Enemy_{enemyStrategy}_{gameTimeString}");
    }

    public void TagVersion(string version) {
        const Tag tag = Tag.Version;

        TagGame(tag, $"v{version}");
    }

    public void TagMinerals(float collectedMinerals) {
        const Tag tag = Tag.Minerals;

        TagGame(tag, $"{tag}_{collectedMinerals}");
    }

    public void TagEnemyRace(Race actualEnemyRace) {
        const Tag tag = Tag.EnemyRace;

        TagGame(tag, $"{tag}_{actualEnemyRace}");
    }

    private void TagGame(Tag tag, string tagString) {
        if (!CanTag(tag)) {
            Logger.Warning($"Trying to tag {tag}: {tagString}, but we can't");
            return;
        }

        var cleanTagString = FormatTag(tagString);
        if (cleanTagString.Length > MaximumTagLength) {
            Logger.Warning($"Tag ${cleanTagString} exceeds the maximum tag length of ${MaximumTagLength}. SC2AIArena will truncate it.");
        }

        Logger.Tag($"Tagging game with {cleanTagString} ({tag})");
        _chatService.Chat($"Tag:{cleanTagString}", toTeam: true);
        _tagsSent.Add(tag);
    }

    private bool CanTag(Tag tag) {
        return tag switch
        {
            Tag.EnemyStrategy => true,
            Tag.EnemyRace => true,
            _ => !HasTagged(tag),
        };
    }

    private static string FormatTag(string tagString) {
        return tagString
            .Replace(" ", "")
            .Replace(".", "_")
            .Replace("-", "_")
            .Replace(":", "_");
    }
}
