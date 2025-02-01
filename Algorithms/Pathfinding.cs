namespace Algorithms;

/// <summary>
/// A collection of pathfinding algorithms.
/// </summary>
public static class Pathfinding {
    /// <summary>
    /// <para>A textbook implementation of the A* search algorithm.</para>
    /// <para>See https://en.wikipedia.org/wiki/A*_search_algorithm</para>
    /// </summary>
    /// <param name="origin">The origin position.</param>
    /// <param name="destination">The destination position.</param>
    /// <param name="getEdgeLength">A function that computes the distance between two vertex.</param>
    /// <param name="getVertexNeighbors">A function that returns the neighbors of a vertex.</param>
    /// <returns>The requested path, or null if the destination is unreachable from the origin.</returns>
    public static List<TVertex>? AStar<TVertex>(
        TVertex origin,
        TVertex destination,
        Func<TVertex, TVertex, float> getEdgeLength,
        Func<TVertex, IEnumerable<TVertex>> getVertexNeighbors
    ) where TVertex : notnull {
        var cameFrom = new Dictionary<TVertex, TVertex>();

        var gScore = new Dictionary<TVertex, float>
        {
            [origin] = 0,
        };

        var fScore = new Dictionary<TVertex, float>
        {
            [origin] = getEdgeLength(origin, destination),
        };

        var openSetContents = new HashSet<TVertex>
        {
            origin,
        };
        var openSet = new PriorityQueue<TVertex, float>();
        openSet.Enqueue(origin, fScore[origin]);

        while (openSet.Count > 0) {
            var current = openSet.Dequeue();
            openSetContents.Remove(current);

            if (EqualityComparer<TVertex>.Default.Equals(current, destination)) {
                return BuildPath(cameFrom, current);
            }

            foreach (var neighbor in getVertexNeighbors(current)) {
                var neighborGScore = gScore[current] + getEdgeLength(current, neighbor);

                if (!gScore.ContainsKey(neighbor) || neighborGScore < gScore[neighbor]) {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = neighborGScore;
                    fScore[neighbor] = neighborGScore + getEdgeLength(current, destination);

                    if (openSetContents.Add(neighbor)) {
                        openSet.Enqueue(neighbor, fScore[neighbor]);
                    }
                }
            }
        }

        return null;
    }

    private static List<TVertex> BuildPath<TVertex>(IReadOnlyDictionary<TVertex, TVertex> cameFrom, TVertex end) {
        var current = end;
        var path = new List<TVertex> { current };
        while (cameFrom.ContainsKey(current)) {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();

        return path;
    }
}
