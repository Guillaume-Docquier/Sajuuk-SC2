using Bot.StateManagement;

namespace Bot.Managers.WarManagement.States;

public abstract class WarManagerState : State<WarManager> {
    public abstract IWarManagerBehaviour Behaviour { get; }
}
