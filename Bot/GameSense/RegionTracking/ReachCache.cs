using System.Collections.Generic;
using System.Linq;
using Bot.MapKnowledge;

namespace Bot.GameSense.RegionTracking;

public class ReachCache {
    private readonly Dictionary<string, Dictionary<Region, float>> _cache = new();

    public bool TryGet(Region startingRegion, IEnumerable<Region> regions, Region blockedRegion, out Dictionary<Region, float> reach) {
        var key = GetKey(startingRegion, regions, blockedRegion);
        if (_cache.ContainsKey(key)) {
            reach = _cache[key];
            return true;
        }

        reach = null;
        return false;
    }

    public void Save(Region startingRegion, IEnumerable<Region> regions, Region blockedRegion, Dictionary<Region, float> reach) {
        var key = GetKey(startingRegion, regions, blockedRegion);
        _cache[key] = reach;
    }

    public void Clear() {
        _cache.Clear();
    }

    private string GetKey(Region startingRegion, IEnumerable<Region> regions, Region blockedRegion = null) {
        return $"{startingRegion.Id}{string.Join("", regions.Select(region => region.Id))}{blockedRegion?.Id}";
    }
}
