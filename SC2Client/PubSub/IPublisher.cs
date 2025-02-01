namespace SC2Client.PubSub;

public interface IPublisher<out TEvent> {
    void Register(Action<TEvent> handler);

    void Deregister(Action<TEvent> handler);
}
