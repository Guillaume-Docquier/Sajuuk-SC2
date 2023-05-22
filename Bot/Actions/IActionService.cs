using System.Collections.Generic;
using SC2APIProtocol;

namespace Bot.Actions;

public interface IActionService {
    public void AddAction(Action action);
    public IEnumerable<Action> GetActions();
}
