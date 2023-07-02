namespace Sajuuk;

public interface ISubscriber<TData> {
    void Notify(TData data);
}
