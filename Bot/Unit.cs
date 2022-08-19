using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.Managers;
using Bot.UnitModules;
using Bot.Wrapper;
using Google.Protobuf.Collections;
using SC2APIProtocol;
using Action = SC2APIProtocol.Action;

namespace Bot;

public class Unit: ICanDie {
    private readonly List<IWatchUnitsDie> _deathWatchers = new List<IWatchUnitsDie>();
    public UnitTypeData UnitTypeData;

    public string Name;
    public ulong Tag;
    public uint UnitType;
    public float FoodRequired;
    public float Radius;
    public SC2APIProtocol.Unit RawUnitData;
    public Alliance Alliance;
    public Vector3 Position;
    private float _buildProgress;
    public RepeatedField<UnitOrder> Orders;
    public bool IsVisible;
    public ulong LastSeen;
    public HashSet<uint> Buffs;
    public int InitialMineralCount = int.MaxValue;
    public int InitialVespeneCount = int.MaxValue;

    public float MaxRange {
        get {
            var weapons = UnitTypeData.Weapons;
            if (weapons.Count <= 0) {
                return 0;
            }

            return UnitTypeData.Weapons.Max(weapon => weapon.Range);
        }
    }

    public ulong DeathDelay = 1;

    private IManager _manager;
    public IManager Manager {
        get => _manager;
        set {
            if (_manager != value) {
                _manager?.Release(this);
                _manager = value;
            }
        }
    }

    // TODO GD Probably implement ISupervisor
    public IManager Supervisor;

    public readonly Dictionary<string, IUnitModule> Modules = new Dictionary<string, IUnitModule>();

    public float Integrity => (RawUnitData.Health + RawUnitData.Shield) / (RawUnitData.HealthMax + RawUnitData.ShieldMax);

    // TODO GD Find a better name
    public float MineralPercent => (float)RawUnitData.MineralContents / InitialMineralCount;

    public float VespenePercent => (float)RawUnitData.VespeneContents / InitialVespeneCount;

    public bool IsOperational => _buildProgress >= 1;

    // Units inside extractors are not available. We keep them in memory but they're not in the game for some time
    public bool IsAvailable => LastSeen >= Controller.Frame;

    public IEnumerable<UnitOrder> OrdersExceptMining => Orders.Where(order => !Abilities.Gather.Contains(order.AbilityId) && !Abilities.ReturnCargo.Contains(order.AbilityId));

    public Unit(SC2APIProtocol.Unit unit, ulong frame) {
        Update(unit, frame);
    }

    public void Update(SC2APIProtocol.Unit unit, ulong frame) {
        UnitTypeData = KnowledgeBase.GetUnitTypeData(unit.UnitType); // Not sure if it can change over time
        RawUnitData = unit;

        Name = UnitTypeData.Name;
        Tag = unit.Tag;
        UnitType = unit.UnitType;
        FoodRequired = UnitTypeData.FoodRequired;
        Radius = unit.Radius;
        Alliance = unit.Alliance;
        Position = new Vector3(unit.Pos.X, unit.Pos.Y, unit.Pos.Z);
        _buildProgress = unit.BuildProgress;
        Orders = unit.Orders;
        IsVisible = unit.DisplayType == DisplayType.Visible;
        LastSeen = frame;
        Buffs = new HashSet<uint>(unit.BuffIds);

        // Snapshot minerals/gas don't have contents
        if (IsVisible && InitialMineralCount == int.MaxValue) {
            InitialMineralCount = RawUnitData.MineralContents;
            InitialVespeneCount = RawUnitData.VespeneContents;
        }
    }

    public double DistanceTo(Unit otherUnit) {
        return DistanceTo(otherUnit.Position);
    }

    public double DistanceTo(Vector3 location) {
        return Position.DistanceTo(location);
    }

    public double HorizontalDistanceTo(Unit otherUnit) {
        return HorizontalDistanceTo(otherUnit.Position);
    }

    public double HorizontalDistanceTo(Vector3 location) {
        return Position.HorizontalDistanceTo(location);
    }

    public void Move(Vector3 target, bool spam = false) {
        if (!spam && IsTargeting(target)) {
            return;
        }

        ProcessAction(ActionBuilder.Move(Tag, target));
    }

    public void Attack(Unit target) {
        if (IsAttacking(target)) {
            return;
        }

        ProcessAction(ActionBuilder.Attack(Tag, target.Tag));
    }

    public void AttackMove(Vector3 target) {
        if (IsTargeting(target)) {
            return;
        }

        if (RawUnitData.IsBurrowed) {
            Move(target);
        }
        else {
            ProcessAction(ActionBuilder.AttackMove(Tag, target));
        }
    }

    public void TrainUnit(uint unitType, bool queue = false) {
        // TODO GD This should be handled when choosing a producer
        if (!queue && Orders.Count > 0) {
            Logger.Error("A unit is trying to train another unit, but it already has another order and queue is false");
            return;
        }

        ProcessAction(ActionBuilder.TrainUnit(unitType, Tag));

        var targetName = KnowledgeBase.GetUnitTypeData(unitType).Name;
        Logger.Info("{0} started training {1}", this, targetName);
    }

    public void UpgradeInto(uint unitOrBuildingType) {
        // You upgrade a unit or building by training the upgrade from the producer
        ProcessAction(ActionBuilder.TrainUnit(unitOrBuildingType, Tag));

        var upgradeName = KnowledgeBase.GetUnitTypeData(unitOrBuildingType).Name;
        Logger.Info("Upgrading {0} into {1}", this, upgradeName);
    }

    public void PlaceBuilding(uint buildingType, Vector3 target) {
        Manager = null;
        ProcessAction(ActionBuilder.PlaceBuilding(buildingType, Tag, target));

        var buildingName = KnowledgeBase.GetUnitTypeData(buildingType).Name;
        Logger.Info("{0} started building {1} at {2}", this, buildingName, target);
    }

    public void PlaceExtractor(uint buildingType, Unit gas) {
        Manager = null;
        ProcessAction(ActionBuilder.PlaceExtractor(buildingType, Tag, gas.Tag));

        var buildingName = KnowledgeBase.GetUnitTypeData(buildingType).Name;
        Logger.Info("{0} started building {1} on gas at {2}", this, buildingName, gas.Position);
    }

    public void ResearchUpgrade(uint upgradeType)
    {
        ProcessAction(ActionBuilder.ResearchUpgrade(upgradeType, Tag));

        var researchName = KnowledgeBase.GetUpgradeData(upgradeType).Name;
        Logger.Info("{0} started researching {1}", this, researchName);
    }

    public void Gather(Unit mineralOrGas) {
        ProcessAction(ActionBuilder.Gather(Tag, mineralOrGas.Tag));
    }

    public void ReturnCargo() {
        ProcessAction(ActionBuilder.ReturnCargo(Tag));
    }

    public void UseAbility(uint abilityId, Point2D position = null, ulong targetUnitTag = ulong.MaxValue) {
        if (Orders.Any(order => order.AbilityId == abilityId)) {
            return;
        }

        if (Abilities.EnergyCost.TryGetValue(abilityId, out var energyCost) && RawUnitData.Energy < energyCost) {
            RawUnitData.Energy -= energyCost;
        }

        ProcessAction(ActionBuilder.UnitCommand(abilityId, Tag, position, targetUnitTag));

        var abilityName = KnowledgeBase.GetAbilityData(abilityId).FriendlyName;
        if (targetUnitTag != ulong.MaxValue) {
            if (!UnitsTracker.UnitsByTag.ContainsKey(targetUnitTag)) {
                Logger.Error("Error with {0} trying to {1} on {2}: The target doesn't exist", this, abilityName, Tag);
                return;
            }

            if (abilityId != Abilities.Smart) {
                var targetUnit = UnitsTracker.UnitsByTag[targetUnitTag];
                Logger.Info("{0} using ability {1} on {2}", this, abilityName, targetUnit);
            }
        }
        else if (position != null) {
            Logger.Info("{0} using ability {1} at {2}", this, abilityName, position.ToVector3().WithWorldHeight());
        }
        else {
            Logger.Info("{0} using ability {1}", this, abilityName);
        }
    }

    private void ProcessAction(Action action) {
        Controller.AddAction(action);

        var order = new UnitOrder
        {
            AbilityId = (uint)action.ActionRaw.UnitCommand.AbilityId,
            TargetUnitTag = action.ActionRaw.UnitCommand.TargetUnitTag,
        };

        if (action.ActionRaw.UnitCommand.TargetWorldSpacePos != null) {
            order.TargetWorldSpacePos = new Point
            {
                X = action.ActionRaw.UnitCommand.TargetWorldSpacePos.X,
                Y = action.ActionRaw.UnitCommand.TargetWorldSpacePos.Y,
                Z = 0, // We don't know
            };
        }

        Orders.Add(order);
    }

    public void AddDeathWatcher(IWatchUnitsDie watcher) {
        _deathWatchers.Add(watcher);
    }

    public void RemoveDeathWatcher(IWatchUnitsDie watcher) {
        _deathWatchers.Remove(watcher);
    }

    public void Died() {
        // Reduce the noise
        if (UnitType is not Units.Larva and not Units.Egg) {
            Logger.Info("{0} died", this);
        }

        // We .ToList() to make a copy of _deathWatchers because some ReportUnitDeath will call RemoveDeathWatcher
        // They shouldn't because it modifies the collection while we are iterating it
        // Also, units die once
        _deathWatchers.ToList().ForEach(watcher => watcher.ReportUnitDeath(this));
    }

    public bool IsDead(ulong atFrame) {
        return atFrame - LastSeen > DeathDelay;
    }

    public void ExecuteModules() {
        foreach (var module in Modules.Values) {
            module.Execute();
        }
    }

    public bool HasEnoughEnergy(uint abilityId) {
        if (Abilities.EnergyCost.TryGetValue(abilityId, out var energyCost) && RawUnitData.Energy < energyCost) {
            return false;
        }

        return true;
    }

    public bool IsIdleOrMovingOrAttacking() {
        return Orders.All(order => order.AbilityId is Abilities.Move or Abilities.Attack);
    }

    // TODO GD Check for move vs attack move, otherwise a move order could be canceled if an attack move order targets the same position
    private bool IsTargeting(Vector3 target) {
        var targetAsPoint = target.ToPoint();
        targetAsPoint.Z = 0;

        return Orders.Any(order => order.TargetWorldSpacePos != null && order.TargetWorldSpacePos.Equals(targetAsPoint));
    }

    private bool IsAttacking(Unit unit) {
        return Orders.Any(order => order.TargetUnitTag == unit.Tag);
    }

    public bool IsEngaging(IReadOnlySet<ulong> unitTags) {
        return unitTags.Contains(RawUnitData.EngagedTargetTag);
    }

    public bool IsProducing(uint buildingOrUnitType) {
        var buildingAbilityId = KnowledgeBase.GetUnitTypeData(buildingOrUnitType).AbilityId;

        return Orders.Any(order => order.AbilityId == buildingAbilityId);
    }

    public override string ToString() {
        return $"{Name}[{Tag}]";
    }
}
