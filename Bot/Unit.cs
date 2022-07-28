using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.GameData;
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

    public ulong DeathDelay = 1;

    public readonly Dictionary<string, IUnitModule> Modules = new Dictionary<string, IUnitModule>();

    public float Integrity => (RawUnitData.Health + RawUnitData.Shield) / (RawUnitData.HealthMax + RawUnitData.ShieldMax);
    public bool IsOperational => _buildProgress >= 1;

    // Units inside extractors are not available. We keep them in memory but they're not in the game for some time
    public bool IsAvailable => LastSeen >= Controller.Frame;

    public IEnumerable<UnitOrder> OrdersExceptMining => Orders.Where(order => order.AbilityId != Abilities.DroneGather && order.AbilityId != Abilities.DroneReturnCargo);

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
    }

    public double DistanceTo(Unit otherUnit) {
        return Vector3.Distance(Position, otherUnit.Position);
    }

    public double DistanceTo(Vector3 location) {
        return Vector3.Distance(Position, location);
    }

    public void Move(Vector3 target) {
        if (IsAlreadyTargeting(target)) {
            return;
        }

        ProcessAction(ActionBuilder.Move(Tag, target));
    }

    public void Attack(Unit target) {
        if (IsAlreadyAttacking(target)) {
            return;
        }

        ProcessAction(ActionBuilder.Attack(Tag, target.Tag));
    }

    public void AttackMove(Vector3 target) {
        if (IsAlreadyTargeting(target)) {
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
            return;
        }

        ProcessAction(ActionBuilder.TrainUnit(unitType, Tag));

        var targetName = KnowledgeBase.GetUnitTypeData(unitType).Name;
        Logger.Info("{0} {1} started training {2}", Name, Tag, targetName);
    }

    public void UpgradeInto(uint unitOrBuildingType) {
        // You upgrade a unit or building by training the upgrade from the producer
        ProcessAction(ActionBuilder.TrainUnit(unitOrBuildingType, Tag));

        var upgradeName = KnowledgeBase.GetUnitTypeData(unitOrBuildingType).Name;
        Logger.Info("Upgrading {0} {1} into {2}", Name, Tag, upgradeName);
    }

    public void PlaceBuilding(uint buildingType, Vector3 target) {
        ProcessAction(ActionBuilder.PlaceBuilding(buildingType, Tag, target));

        var buildingName = KnowledgeBase.GetUnitTypeData(buildingType).Name;
        Logger.Info("{0} {1} started building {2} at [{3}, {4}]", Name, Tag, buildingName, target.X, target.Y);
    }

    public void PlaceExtractor(uint buildingType, Unit gas)
    {
        ProcessAction(ActionBuilder.PlaceExtractor(buildingType, Tag, gas.Tag));

        var buildingName = KnowledgeBase.GetUnitTypeData(buildingType).Name;
        Logger.Info("{0} {1} started building {2} on gas at [{3}, {4}]", Name, Tag, buildingName, gas.Position.X, gas.Position.Y);
    }

    public void ResearchUpgrade(uint upgradeType)
    {
        ProcessAction(ActionBuilder.ResearchUpgrade(upgradeType, Tag));

        var researchName = KnowledgeBase.GetUpgradeData(upgradeType).Name;
        Logger.Info("{0} {1} started researching {2}", Name, Tag, researchName);
    }

    public void Gather(Unit mineralOrGas) {
        ProcessAction(ActionBuilder.Gather(Tag, mineralOrGas.Tag));
    }

    public void ReturnCargo(Unit @base) {
        ProcessAction(ActionBuilder.ReturnCargo(Tag, @base.Tag));

        Logger.Info("{0} {1} returning cargo to {2} {3} at [{4}, {5}]", Name, Tag, @base.Name, @base.Tag, @base.Position.X, @base.Position.Y);
    }

    public void UseAbility(int abilityId, Point2D position = null, ulong targetUnitTag = ulong.MaxValue) {
        if (Orders.Any(order => order.AbilityId == abilityId)) {
            return;
        }

        if (Abilities.EnergyCost.TryGetValue(abilityId, out var energyCost) && RawUnitData.Energy < energyCost) {
            RawUnitData.Energy -= energyCost;
        }

        ProcessAction(ActionBuilder.UnitCommand(abilityId, Tag, position, targetUnitTag));

        var abilityName = KnowledgeBase.GetAbilityData(abilityId).FriendlyName;
        if (targetUnitTag != ulong.MaxValue) {
            var targetUnit = Controller.UnitsByTag[targetUnitTag];
            Logger.Info("{0} {1} using ability {2} on {3} {4}", Name, Tag, abilityName, targetUnit.Name, targetUnit.Tag);
        }
        else if (position != null) {
            Logger.Info("{0} {1} using ability {2} at [{4}, {5}]", Name, Tag, abilityName, position.X, position.Y);
        }
        else {
            Logger.Info("{0} {1} using ability {2}", Name, Tag, abilityName);
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
        if (UnitType != Units.Larva) {
            Logger.Info("{0} {1} died", Name, Tag);
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

    public bool HasEnoughEnergy(int abilityId) {
        if (Abilities.EnergyCost.TryGetValue(abilityId, out var energyCost) && RawUnitData.Energy < energyCost) {
            return false;
        }

        return true;
    }

    public bool IsMovingOrAttacking() {
        return Orders.All(order => order.AbilityId is Abilities.Move or Abilities.Attack);
    }

    private bool IsAlreadyTargeting(Vector3 target) {
        var targetAsPoint = target.ToPoint();
        targetAsPoint.Z = 0;

        return Orders.Any(order => order.TargetWorldSpacePos != null && order.TargetWorldSpacePos.Equals(targetAsPoint));
    }

    private bool IsAlreadyAttacking(Unit unit) {
        return Orders.Any(order => order.TargetUnitTag == unit.Tag);
    }

    public bool IsBuilding(uint buildingType) {
        var buildingAbilityId = KnowledgeBase.GetUnitTypeData(buildingType).AbilityId;

        return Orders.Any(order => order.AbilityId == buildingAbilityId);
    }
}
