using System.Threading.Tasks;
using SC2APIProtocol;

namespace Sajuuk.Wrapper;

public interface IGame {
    /// <summary>
    /// Sets up the game so that it is ready to join.
    /// </summary>
    public Task Setup();

    /// <summary>
    /// Joins
    /// </summary>
    /// <param name="race"></param>
    /// <returns></returns>
    public Task<uint> Join(Race race);
}
