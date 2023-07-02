namespace Sajuuk;

// TODO GD Use a publishing server and have consumers register to topics by providing a delegate handler?
public interface IPublisher<TData> {
    void Register(ISubscriber<TData> subscriber);

    void UnRegister(ISubscriber<TData> subscriber);
}
