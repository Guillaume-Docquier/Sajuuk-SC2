using System.Numerics;
using System.Text.Json.Serialization;
using Algorithms.ExtensionMethods;
using SC2APIProtocol;
using SC2Client.ExtensionMethods;
using SC2Client.GameData;
using SC2Client.Logging;
using SC2Client.PubSub.Events;

namespace SC2Client.State;

// TODO GD I might want to split this up between RawUnit and Unit. This way I can have the "simple" unit and the "complicated" unit.
public sealed class Unit : IUnit {
    private readonly ILogger _logger;

    [JsonInclude] public Vector3 Position { get; init; }
    [JsonInclude] public ulong Tag { get; init; }
    [JsonInclude] public string Name { get; init; }
    [JsonInclude] public uint UnitType { get; init; }
    [JsonInclude] public float FoodRequired { get; init; }
    [JsonInclude] public float Radius { get; init; }
    [JsonInclude] public Alliance Alliance { get; init; }
    [JsonInclude] public bool IsSnapshot { get; init; }
    [JsonInclude] public bool IsFlying { get; init; }
    [JsonInclude] public bool IsCloaked { get; init; }
    [JsonInclude] public bool IsBurrowed { get; init; }
    [JsonInclude] public ulong LastSeen { get; init; }

    private readonly HashSet<Action<UnitDeath>> _deathHandlers = new HashSet<Action<UnitDeath>>();

    [JsonConstructor]
    [Obsolete("Do not use this parameterless JsonConstructor", error: true)]
#pragma warning disable CS8618, CS9264
    public Unit() {}
#pragma warning restore CS8618, CS9264

    public Unit(KnowledgeBase knowledgeBase, ulong currentFrame, SC2APIProtocol.Unit rawUnit, ILogger logger) {
        var unitTypeData = knowledgeBase.GetUnitTypeData(rawUnit.UnitType);

        Position = rawUnit.Pos.ToVector3();
        Tag = rawUnit.Tag;
        Name = unitTypeData.Name;
        UnitType = rawUnit.UnitType;
        FoodRequired = unitTypeData.FoodRequired;
        Radius = rawUnit.Radius;
        Alliance = rawUnit.Alliance;
        IsSnapshot = rawUnit.DisplayType == DisplayType.Snapshot;
        IsFlying = rawUnit.IsFlying;
        IsCloaked = rawUnit.Cloak == CloakState.Cloaked;
        IsBurrowed = rawUnit.IsBurrowed;
        LastSeen = currentFrame;

        // Needs to be last otherwise some properties might not be set
        _logger = logger.CreateNamed($"{ToString()}");
    }

    // TODO GD Update for LastSeen / IsSnapshot

    public float Distance2DTo(Vector2 position) {
        return Position.ToVector2().DistanceTo(position);
    }

    public float Distance2DTo(IUnit otherUnit) {
        return Distance2DTo(otherUnit.Position.ToVector2());
    }

    public override string ToString() {
        return Alliance == Alliance.Self
            ? $"{Name}[{Tag}]"
            : $"{Alliance} {Name}[{Tag}]";
    }

    public void Register(Action<UnitDeath> handler) {
        _logger.Warning("Unit death is not implemented yet!");
        _deathHandlers.Add(handler);
    }

    public void Deregister(Action<UnitDeath> handler) {
        _logger.Warning("Unit death is not implemented yet!");
        _deathHandlers.Remove(handler);
    }
}
