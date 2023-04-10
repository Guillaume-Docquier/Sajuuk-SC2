using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Bot.ExtensionMethods;

namespace Bot.MapKnowledge;

public class ExpandLocation : IWatchUnitsDie {
    public Vector2 Position { get; }

    [JsonIgnore]
    private HashSet<Unit> _resourceCluster;
    [JsonIgnore]
    public IReadOnlySet<Unit> ResourceCluster => _resourceCluster;
    [JsonIgnore]
    public bool IsDepleted => !ResourceCluster.Any();

    [JsonIgnore]
    private HashSet<Unit> _blockers;
    [JsonIgnore]
    public IReadOnlySet<Unit> Blockers => _blockers;
    [JsonIgnore]
    public bool IsObstructed => Blockers.Any();

    public ExpandType ExpandType { get; }

    public ExpandLocation(Vector2 position, ExpandType expandType) {
        Position = position;
        ExpandType = expandType;
    }

    public void Init(HashSet<Unit> resourceCluster, HashSet<Unit> blockers) {
        // TODO GD Handle depleted gasses
        _resourceCluster = resourceCluster;
        foreach (var resource in _resourceCluster) {
            resource.AddDeathWatcher(this);
        }

        _blockers = blockers;
        foreach (var blocker in _blockers) {
            blocker.AddDeathWatcher(this);
        }
    }

    public void ReportUnitDeath(Unit deadUnit) {
        if (_blockers.Contains(deadUnit)) {
            _blockers.Remove(deadUnit);
        }
        else if (ResourceCluster.Contains(deadUnit)) {
            _resourceCluster.Remove(deadUnit);
        }
    }

    public void Clear() {
        // TODO
    }

    public IRegion GetRegion() {
        return Position.GetRegion();
    }
}
