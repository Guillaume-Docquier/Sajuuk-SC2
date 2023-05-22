namespace Bot.Persistence;

public interface IMapDataRepository<TData> {
    public void Save(TData data, string mapFileName);
    public TData Load(string mapFileName);
}
