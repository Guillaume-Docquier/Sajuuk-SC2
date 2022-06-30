namespace Bot;

public interface ICanDie {
    void AddWatcher(IWatchUnitsDie watcher);

    void Died();
}
