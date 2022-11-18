using System.Collections.Generic;
using System.Numerics;
using Bot.MapKnowledge;

namespace Bot.ExtensionMethods;

public static class PathExtensions {
    public static float GetPathDistance(this IList<Vector2> path) {
        var distance = 0f;
        for (var i = 0; i < path.Count - 1; i++) {
            distance += path[i].DistanceTo(path[i + 1]);
        }

        return distance;
    }

    public static float GetPathDistance(this IList<Region> path) {
        var distance = 0f;
        for (var i = 0; i < path.Count - 1; i++) {
            distance += path[i].Center.DistanceTo(path[i + 1].Center);
        }

        return distance;
    }
}
