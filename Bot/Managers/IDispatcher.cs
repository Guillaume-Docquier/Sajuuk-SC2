namespace Bot.Managers;

/// <summary>
/// A Dispatcher assigns Supervisors to units.
/// </summary>
public interface IDispatcher {
    void Dispatch(Unit unit);
}
