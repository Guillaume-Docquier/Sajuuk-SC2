using System.Threading.Tasks;
using SC2APIProtocol;

namespace Bot.Wrapper;

public interface IGame {
    /// <summary>
    /// Sets up the game so that it is ready to join.
    /// </summary>
    /// <returns>Nothing</returns>
    public Task Setup();

    /// <summary>
    /// Joins
    /// </summary>
    /// <param name="race"></param>
    /// <returns></returns>
    public Task<uint> Join(Race race);
}
