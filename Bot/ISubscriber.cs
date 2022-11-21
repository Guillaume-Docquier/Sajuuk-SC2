namespace Bot;

public interface ISubscriber<TData> {
    void Notify(TData data);
}
