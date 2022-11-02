using System.Threading.Tasks;
using SC2APIProtocol;

namespace Bot.Wrapper;

public interface IBot {
    string Name { get; }

    Race Race { get; }

    Task OnFrame();
}
