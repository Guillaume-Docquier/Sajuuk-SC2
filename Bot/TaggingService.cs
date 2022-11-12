using System.Collections.Generic;
using Bot.Utils;

namespace Bot;

public static class TaggingService {
    private static readonly HashSet<Tag> TagsSent = new HashSet<Tag>();

    public enum Tag {
        EarlyAttack,
        BuildDone,
        EnemyStrategy,
        Version,
    }

    public static void TagGame(Tag tag, params object[] parameters) {
        if (!CanTag(tag)) {
            return;
        }

        var tagString = CleanTag(FormatTag(tag, parameters));

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
        return tag switch
        {
            Tag.EarlyAttack => $"{tag:G}_{TimeUtils.GetGameTimeString()}",
            Tag.BuildDone => $"{tag:G}_{TimeUtils.GetGameTimeString()}_Supply_{Controller.CurrentSupply}",
            Tag.EnemyStrategy => $"{tag:G}_{parameters[0]}_{TimeUtils.GetGameTimeString()}",
            Tag.Version => $"v{parameters[0]}",
            _ => tag.ToString("G"),
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
