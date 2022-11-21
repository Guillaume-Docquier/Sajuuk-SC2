namespace Bot;

// TODO GD Replace by IPublisher<Unit deadUnit>
public interface ICanDie {
    void AddDeathWatcher(IWatchUnitsDie watcher);

    void RemoveDeathWatcher(IWatchUnitsDie watcher);

    void Died();
}
