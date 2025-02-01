using System.Numerics;
using Algorithms.ExtensionMethods;
using SC2APIProtocol;
using SC2Client.ExtensionMethods;
using SC2Client.GameData;
using SC2Client.PubSub.Events;

namespace SC2Client.State;

// TODO GD I might want to split this up between RawUnit and Unit. This way I can have the "simple" unit and the "complicated" unit.
public sealed class Unit : IUnit {
    private readonly ILogger _logger;

    public Vector3 Position { get; }
    public ulong Tag { get; }
    public string Name { get; }
    public uint UnitType { get; }
    public float FoodRequired { get; }
    public float Radius { get; }
    public Alliance Alliance { get; }
    public bool IsSnapshot { get; }
    public bool IsFlying { get; }
    public bool IsCloaked { get; }
    public bool IsBurrowed { get; }
    public ulong LastSeen { get; }

    private readonly HashSet<Action<UnitDeath>> _deathHandlers = new HashSet<Action<UnitDeath>>();

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
