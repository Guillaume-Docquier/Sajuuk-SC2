using System.Collections.Generic;
using SC2APIProtocol;
using Action = SC2APIProtocol.Action;

namespace Bot;

public class ActionService : IActionService, INeedUpdating {
    private readonly List<Action> _actions = new List<Action>();

    public void AddAction(Action action) {
        _actions.Add(action);
    }

    public IEnumerable<Action> GetActions() {
        return _actions;
    }

    public void Update(ResponseObservation observation, ResponseGameInfo gameInfo) {
        _actions.Clear();
    }
}
