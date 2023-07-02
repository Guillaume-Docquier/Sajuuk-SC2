using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Sajuuk.MapAnalysis.RegionAnalysis;

namespace Sajuuk.MapAnalysis.ExpandAnalysis;

public class ExpandLocation : IExpandLocation, IWatchUnitsDie {
    [JsonInclude] public Vector2 Position { get; private set;}

    [JsonIgnore] public HashSet<Unit> Resources { get; set; }
    [JsonIgnore] public bool IsDepleted => !Resources.Any();

    [JsonIgnore] public HashSet<Unit> Blockers { get; set; }
    [JsonIgnore] public bool IsBlocked => Blockers.Any();

    [JsonInclude] public IRegion Region { get; set; }

    [JsonInclude] public ExpandType ExpandType { get; private set;}

    [JsonConstructor] public ExpandLocation() {}

    public ExpandLocation(Vector2 position, ExpandType expandType) {
        Position = position;
        ExpandType = expandType;
    }

    public void SetResources(HashSet<Unit> resourceCluster) {
        // TODO GD Handle depleted gasses
        Resources = resourceCluster;
        foreach (var resource in Resources) {
            resource.AddDeathWatcher(this);
        }
    }

    public void SetBlockers(HashSet<Unit> blockers) {
        Blockers = blockers;
        foreach (var blocker in Blockers) {
            blocker.AddDeathWatcher(this);
        }
    }

    public void ReportUnitDeath(Unit deadUnit) {
        if (Blockers.Contains(deadUnit)) {
            Blockers.Remove(deadUnit);
        }
        else if (Resources.Contains(deadUnit)) {
            Blockers.Remove(deadUnit);
        }
    }
}
