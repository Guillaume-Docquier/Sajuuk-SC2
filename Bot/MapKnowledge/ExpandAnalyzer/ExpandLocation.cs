using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;

namespace Bot.MapKnowledge;

// TODO GD Split this into two types
public class ExpandLocation : IExpandLocation, IWatchUnitsDie {
    public Vector2 Position { get; }

    [JsonIgnore]
    private HashSet<Unit> _resources;
    [JsonIgnore]
    public IReadOnlySet<Unit> Resources => _resources;
    [JsonIgnore]
    public bool IsDepleted => !Resources.Any();

    [JsonIgnore]
    private HashSet<Unit> _blockers;
    [JsonIgnore]
    public IReadOnlySet<Unit> Blockers => _blockers;
    [JsonIgnore]
    public bool IsBlocked => Blockers.Any();

    public ExpandType ExpandType { get; }

    public ExpandLocation(Vector2 position, ExpandType expandType) {
        Position = position;
        ExpandType = expandType;
    }

    public void Init(HashSet<Unit> resourceCluster, HashSet<Unit> blockers) {
        // TODO GD Handle depleted gasses
        _resources = resourceCluster;
        foreach (var resource in _resources) {
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
        else if (Resources.Contains(deadUnit)) {
            _resources.Remove(deadUnit);
        }
    }

    public void Clear() {
        // TODO
    }
}
