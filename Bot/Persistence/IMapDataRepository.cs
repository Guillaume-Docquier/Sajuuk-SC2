namespace Bot.Persistence;

public interface IMapDataRepository<TData> {
    public void Save(TData data);
    public TData Load();
}
