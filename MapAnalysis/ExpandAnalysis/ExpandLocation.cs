using System.Numerics;
using System.Text.Json.Serialization;
using SC2Client.State;

namespace MapAnalysis.ExpandAnalysis;

public class ExpandLocation : IExpandLocation, IWatchUnitsDie {
    [JsonInclude] public Vector2 Position { get; private set;}

    [JsonIgnore] public HashSet<IUnit> Resources { get; set; }
    [JsonIgnore] public bool IsDepleted => !Resources.Any();

    [JsonIgnore] public HashSet<IUnit> Blockers { get; set; }
    [JsonIgnore] public bool IsBlocked => Blockers.Any();

    [JsonInclude] public IRegion Region { get; set; }

    [JsonInclude] public ExpandType ExpandType { get; private set;}

    [JsonConstructor] public ExpandLocation() {}

    public ExpandLocation(Vector2 position, ExpandType expandType, HashSet<IUnit> resources) {
        Position = position;
        ExpandType = expandType;
        Resources = resources;
    }

    public void SetResources(HashSet<IUnit> resourceCluster) {
        // TODO GD Handle depleted gasses
        Resources = resourceCluster;
        foreach (var resource in Resources) {
            resource.AddDeathWatcher(this);
        }
    }

    public void SetBlockers(HashSet<IUnit> blockers) {
        Blockers = blockers;
        foreach (var blocker in Blockers) {
            blocker.AddDeathWatcher(this);
        }
    }

    public void ReportUnitDeath(IUnit deadUnit) {
        if (Blockers.Contains(deadUnit)) {
            Blockers.Remove(deadUnit);
        }
        else if (Resources.Contains(deadUnit)) {
            Blockers.Remove(deadUnit);
        }
    }
}
