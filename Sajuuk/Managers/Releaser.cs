namespace Sajuuk.Managers;

public abstract class Releaser<TClient> : IReleaser {
    protected TClient Client;

    protected Releaser(TClient client) {
        Client = client;
    }

    public abstract void Release(Unit unit);
}
