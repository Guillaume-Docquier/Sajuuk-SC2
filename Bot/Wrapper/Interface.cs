using System.Collections.Generic;
using System.Threading.Tasks;
using SC2APIProtocol;

namespace Bot.Wrapper;

public interface IBot {
    string Name { get; }

    Race Race { get; }

    // TODO GD Probably don't make this async? Make a debug data collection step instead?
    Task<IEnumerable<Action>> OnFrame();
}
