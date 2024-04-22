using SC2APIProtocol;

namespace SC2Client;

public interface IGameConnection {
    /// <summary>
    /// Joins as game as the given race.
    /// </summary>
    /// <param name="race">The race to play as</param>
    /// <returns></returns>
    public Task<uint> JoinGame(Race race);
}
