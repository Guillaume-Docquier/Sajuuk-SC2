namespace Bot;

public interface ICanDie {
    void AddDeathWatcher(IWatchUnitsDie watcher);

    void RemoveDeathWatcher(IWatchUnitsDie watcher);

    void Died();
}
