using System.Collections.Generic;
using SC2APIProtocol;

namespace Bot.Wrapper {
    public interface IBot {
        IEnumerable<Action> OnFrame();
    }
}
