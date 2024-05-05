using SC2APIProtocol;

namespace SC2Client;

/// <summary>
/// An interface to join an SC2 game.
/// </summary>
public interface IGameConnection {
    /// <summary>
    /// Joins a game as the given race.
    /// </summary>
    /// <param name="race">The race to play as.</param>
    /// <returns>The player id in the game.</returns>
    public Task<IGame> JoinGame(Race race);
}
