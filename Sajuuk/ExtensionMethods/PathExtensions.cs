﻿using System.Collections.Generic;
using System.Numerics;
using Sajuuk.MapAnalysis.RegionAnalysis;

namespace Sajuuk.ExtensionMethods;

public static class PathExtensions {
    public static float GetPathDistance(this IList<Vector2> path) {
        var distance = 0f;
        for (var i = 0; i < path.Count - 1; i++) {
            distance += path[i].DistanceTo(path[i + 1]);
        }

        return distance;
    }

    public static float GetPathDistance(this IList<IRegion> path) {
        var distance = 0f;
        for (var i = 0; i < path.Count - 1; i++) {
            distance += path[i].Center.DistanceTo(path[i + 1].Center);
        }

        return distance;
    }
}
