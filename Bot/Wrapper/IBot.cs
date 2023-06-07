using System.Threading.Tasks;
using SC2APIProtocol;

namespace Bot.Wrapper;

public interface IBot {
    Race Race { get; }

    Task OnFrame();
}
