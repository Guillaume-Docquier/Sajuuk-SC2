using System.Numerics;
using Algorithms.ExtensionMethods;
using SC2APIProtocol;
using SC2Client.ExtensionMethods;
using SC2Client.GameData;

namespace SC2Client.State;

public class Unit : IUnit {
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

    public Unit(KnowledgeBase knowledgeBase, ulong currentFrame, SC2APIProtocol.Unit rawUnit) {
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
}
