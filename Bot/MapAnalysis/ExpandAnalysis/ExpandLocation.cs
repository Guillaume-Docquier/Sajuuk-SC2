using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Bot.MapAnalysis.RegionAnalysis;

namespace Bot.MapAnalysis.ExpandAnalysis;

// TODO GD Split this into two types
public class ExpandLocation : IExpandLocation, IWatchUnitsDie {
    public int Id { get; }
    public Vector2 Position { get; }

    [JsonIgnore] public HashSet<Unit> Resources { get; set; }
    [JsonIgnore] public bool IsDepleted => !Resources.Any();

    [JsonIgnore] public HashSet<Unit> Blockers { get; set; }
    [JsonIgnore] public bool IsBlocked => Blockers.Any();

    public IRegion Region { get; set; }

    public ExpandType ExpandType { get; }

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
