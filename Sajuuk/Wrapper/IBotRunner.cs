using System.Threading.Tasks;

namespace Sajuuk.Wrapper;

public interface IBotRunner {
    /// <summary>
    /// Sets up and plays a game with the given bot.
    /// </summary>
    /// <param name="bot">The bot that plays.</param>
    /// <returns>A task that completes when the bot is done playing.</returns>
    public Task RunBot(IBot bot);
}
