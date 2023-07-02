using System.Threading.Tasks;
using SC2APIProtocol;

namespace Sajuuk.Wrapper;

public interface IBot {
    Race Race { get; }

    Task OnFrame();
}
