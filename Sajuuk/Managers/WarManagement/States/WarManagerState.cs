using Sajuuk.StateManagement;

namespace Sajuuk.Managers.WarManagement.States;

public abstract class WarManagerState : State<WarManager> {
    public abstract IWarManagerBehaviour Behaviour { get; }
}
