using System.Numerics;
using SC2APIProtocol;
using SC2Client.ExtensionMethods;
using SC2Client.GameData;

namespace SC2Client.State;

public class Unit : IUnit {
    public Vector3 Position { get; private set; }
    public ulong Tag { get; private set; }
    public string Name { get; private set; }
    public uint UnitType { get; private set; }
    public float FoodRequired { get; private set; }
    public float Radius { get; private set; }
    public Alliance Alliance { get; private set; }
    public bool IsVisible { get; private set; }
    public bool IsFlying { get; private set; }
    public bool IsCloaked { get; private set; }
    public bool IsBurrowed { get; private set; }
    public ulong LastSeen { get; private set; }

    public Unit(SC2APIProtocol.Unit rawUnit, ulong currentFrame, KnowledgeBase knowledgeBase) {
        var unitTypeData = knowledgeBase.GetUnitTypeData(rawUnit.UnitType);
        Name = unitTypeData.Name;

        Update(rawUnit, currentFrame, knowledgeBase);
    }

    public void Update(SC2APIProtocol.Unit rawUnit, ulong currentFrame, KnowledgeBase knowledgeBase) {
        var unitTypeChanged = rawUnit.UnitType != UnitType;

        // _buildProgress = rawUnit.BuildProgress;

        Tag = rawUnit.Tag;
        UnitType = rawUnit.UnitType;
        Radius = rawUnit.Radius;
        Alliance = rawUnit.Alliance;
        Position = rawUnit.Pos.ToVector3();
        // Orders = rawUnit.Orders;
        IsVisible = rawUnit.DisplayType == DisplayType.Visible; // TODO GD This is not actually visible as in cloaked
        // Buffs = new HashSet<uint>(rawUnit.BuffIds);

        IsFlying = rawUnit.IsFlying;
        IsBurrowed = rawUnit.IsBurrowed;
        IsCloaked = rawUnit.Cloak == CloakState.Cloaked;

        LastSeen = currentFrame;

        if (unitTypeChanged) {
            var unitTypeData = knowledgeBase.GetUnitTypeData(rawUnit.UnitType);
            Name = unitTypeData.Name;
            FoodRequired = unitTypeData.FoodRequired;

            // AliasUnitTypeData = unitTypeData.HasUnitAlias ? knowledgeBase.GetUnitTypeData(unitTypeData.UnitAlias) : null;

            // UpdateWeaponsData(unitTypeData.Weapons.ToList());
        }

        // WeaponCooldownPercent = HasWeapons
        //     ? RawUnitData.WeaponCooldown / _maxWeaponCooldownFrames
        //     : 1;

        // IsReadyToAttack = WeaponCooldownPercent <= 0;

        // It looks like snapshotted units don't have HP data
        // We won't update in that case, so that we remember what was last seen
        // if (RawUnitData.HealthMax + RawUnitData.ShieldMax > 0) {
        //     HitPoints = RawUnitData.Health + RawUnitData.Shield;
        //     MaxHitPoints = RawUnitData.HealthMax + RawUnitData.ShieldMax;
        //     Integrity = HitPoints / MaxHitPoints;
        // }

        // Snapshot minerals/gas don't have contents
        // if (IsVisible && InitialMineralCount == int.MaxValue) {
        //     InitialMineralCount = RawUnitData.MineralContents;
        //     InitialVespeneCount = RawUnitData.VespeneContents;
        // }
    }

    public override string ToString() {
        if (Alliance == Alliance.Self) {
            return $"{Name}[{Tag}]";
        }

        return $"{Alliance} {Name}[{Tag}]";
    }
}
