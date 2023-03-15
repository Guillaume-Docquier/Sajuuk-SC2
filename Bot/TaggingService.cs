using System.Collections.Generic;
using Bot.Utils;

namespace Bot;

public static class TaggingService {
    private static readonly HashSet<Tag> TagsSent = new HashSet<Tag>();
    private const int MaximumTagLength = 32;

    public enum Tag {
        EarlyAttack,
        BuildDone,
        EnemyStrategy,
        Version,
        TerranFinisher,
        Minerals,
    }

    public static void TagGame(Tag tag, params object[] parameters) {
        if (!CanTag(tag)) {
            return;
        }

        var tagString = CleanTag(FormatTag(tag, parameters));
        if (tagString.Length > MaximumTagLength) {
            Logger.Warning("Tag {0} exceeds the maximum tag length of {1}. SC2AIArena will truncate it.", tagString, MaximumTagLength);
        }

        Logger.Tag("Tagging game with {0}", tagString);
        Controller.Chat($"Tag:{tagString}", toTeam: true);
        TagsSent.Add(tag);
    }

    private static bool CanTag(Tag tag) {
        return tag switch
        {
            Tag.EnemyStrategy => true,
            _ => !TagsSent.Contains(tag),
        };
    }

    private static string FormatTag(Tag tag, params object[] parameters) {
        var gameTimeString = TimeUtils.GetGameTimeString(Controller.Frame);

        return tag switch
        {
            Tag.TerranFinisher => $"{tag}_{gameTimeString}",
            Tag.EarlyAttack => $"{tag}_{gameTimeString}",
            Tag.BuildDone => $"{tag}_{gameTimeString}_Supply_{Controller.CurrentSupply}",

            // We print "EnemyStrategy" as "Enemy" to have more space for the enemy strategy name, otherwise it gets truncated
            Tag.EnemyStrategy => $"Enemy_{parameters[0]}_{gameTimeString}",

            Tag.Version => $"v{parameters[0]}",
            Tag.Minerals => $"{tag}_{parameters[0]}",
            _ => tag.ToString(),
        };
    }

    private static string CleanTag(string tag) {
        return tag
            .Replace(" ", "")
            .Replace(".", "_")
            .Replace("-", "_")
            .Replace(":", "_");
    }
}
