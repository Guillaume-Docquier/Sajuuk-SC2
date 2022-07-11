using System.Collections.Generic;
using SC2APIProtocol;

namespace Bot.Wrapper;

public interface IBot {
    string Name { get; }

    Race Race { get; }

    void OnFrame();
}
