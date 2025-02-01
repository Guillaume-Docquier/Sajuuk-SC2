namespace SC2Client.Services;

/// <summary>
/// A multi layer cache for storing pathfinding paths.
/// A slot key is used to access different cache layers when pathfinding using different constraints.
///
/// This cache is optimized by assuming that a path can be reversed and still be the shortest path.
/// Sub paths are also assumed to be the shortest paths.
/// </summary>
/// <typeparam name="TVertex">The type of the vertices.</typeparam>
public class PathfinderCache<TVertex> : IPathfinderCache<TVertex> where TVertex : notnull {
    /// <summary>
    /// The paths memory in the form of memory[slot][origin][destination] = path_from_origin_to_destination
    /// </summary>
    private readonly Dictionary<string, Dictionary<TVertex, Dictionary<TVertex, List<TVertex>?>>> _memory = new();

    public bool TryGet(string slotKey, TVertex origin, TVertex destination, out List<TVertex>? path) {
        if (!_memory.TryGetValue(slotKey, out var slot)) {
            path = null;
            return false;
        }

        if (slot.ContainsKey(origin) && slot[origin].ContainsKey(destination)) {
            path = slot[origin][destination];
            return true;
        }

        path = null;
        return false;
    }

    public void Save(string slotKey, TVertex origin, TVertex destination, List<TVertex>? path) {
        if (!_memory.ContainsKey(slotKey)) {
            _memory[slotKey] = new Dictionary<TVertex, Dictionary<TVertex, List<TVertex>?>>();
        }

        var slot = _memory[slotKey];

        if (!slot.ContainsKey(origin)) {
            slot[origin] = new Dictionary<TVertex, List<TVertex>?>();
        }

        if (!slot.ContainsKey(destination)) {
            slot[destination] = new Dictionary<TVertex, List<TVertex>?>();
        }

        // TODO GD Also save sub paths
        slot[origin][destination] = path;
        slot[destination][origin] = path == null
            ? null
            : Enumerable.Reverse(path).ToList();
    }
}
