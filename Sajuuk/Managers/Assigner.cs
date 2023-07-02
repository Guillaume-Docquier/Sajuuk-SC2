namespace Sajuuk.Managers;

public abstract class Assigner<TClient> : IAssigner {
    protected TClient Client;

    protected Assigner(TClient client) {
        Client = client;
    }

    public abstract void Assign(Unit unit);
}
