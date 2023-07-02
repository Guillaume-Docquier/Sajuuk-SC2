namespace Sajuuk.Managers;

public abstract class Dispatcher<TClient> : IDispatcher {
    protected TClient Client;

    protected Dispatcher(TClient client) {
        Client = client;
    }

    public abstract void Dispatch(Unit unit);
}
