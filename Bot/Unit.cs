using System.Collections.Generic;
using System.Numerics;
using Bot.UnitModules;
using Bot.Wrapper;
using Google.Protobuf.Collections;
using SC2APIProtocol;
using Action = SC2APIProtocol.Action;

namespace Bot;

public class Unit: ICanDie {
    private readonly List<IWatchUnitsDie> _deathWatchers = new List<IWatchUnitsDie>();
    private readonly UnitTypeData _unitTypeData;

    public string Name;
    public ulong Tag;
    public uint UnitType;
    public float HealthMax;
    public float ShieldMax;
    public int Supply;
    public float Radius;
    public SC2APIProtocol.Unit RawUnitData;
    public Alliance Alliance;
    public Vector3 Position;
    public float Health;
    public float Shield;
    private float _buildProgress;
    public RepeatedField<UnitOrder> Orders;
    public bool IsVisible;
    public int IdealWorkerCount;
    public int AssignedWorkers;
    public ulong LastSeen;
    public HashSet<uint> Buffs;

    public ulong DeathDelay = 1;

    public readonly Dictionary<string, IUnitModule> Modules = new Dictionary<string, IUnitModule>();

    public float Integrity => (RawUnitData.Health + RawUnitData.Shield) / (RawUnitData.HealthMax + RawUnitData.ShieldMax);
    public bool IsOperational => _buildProgress >= 1;

    public Unit(SC2APIProtocol.Unit unit, ulong frame) {
        _unitTypeData = GameData.GetUnitTypeData(unit.UnitType);

        Update(unit, frame);
    }

    public void Update(SC2APIProtocol.Unit unit, ulong frame) {
        RawUnitData = unit;

        Name = _unitTypeData.Name;
        Tag = unit.Tag;
        UnitType = unit.UnitType;
        HealthMax = unit.HealthMax;
        ShieldMax = unit.ShieldMax;
        Supply = (int)_unitTypeData.FoodRequired;
        Radius = unit.Radius;

        Alliance = unit.Alliance; // Alliance can probably change if being mind controlled?
        Position = new Vector3(unit.Pos.X, unit.Pos.Y, unit.Pos.Z);
        Health = unit.Health;
        Shield = unit.Shield;
        _buildProgress = unit.BuildProgress;
        Orders = unit.Orders;
        IsVisible = unit.DisplayType == DisplayType.Visible;
        IdealWorkerCount = unit.IdealHarvesters;
        AssignedWorkers = unit.AssignedHarvesters;
        LastSeen = frame;
        Buffs = new HashSet<uint>(unit.BuffIds);
    }

    public double DistanceTo(Unit otherUnit) {
        return Vector3.Distance(Position, otherUnit.Position);
    }

    public double DistanceTo(Vector3 location) {
        return Vector3.Distance(Position, location);
    }

    private void FocusCamera() {
        var action = new Action
        {
            ActionRaw = new ActionRaw
            {
                CameraMove = new ActionRawCameraMove
                {
                    CenterWorldSpace = new Point
                    {
                        X = Position.X,
                        Y = Position.Y,
                        Z = Position.Z
                    }
                }
            }
        };

        Controller.AddAction(action);
    }

    public void Move(Vector3 target) {
        Controller.AddAction(ActionBuilder.Move(Tag, target));
    }

    public void AttackMove(Vector3 target) {
        Controller.AddAction(ActionBuilder.AttackMove(Tag, target));
    }

    public void Smart(Unit unit) {
        Controller.AddAction(ActionBuilder.Smart(Tag, unit.Tag));
    }

    public void TrainUnit(uint unitType, bool queue = false) {
        // TODO GD This should be handled when choosing a producer
        if (!queue && Orders.Count > 0) {
            return;
        }

        ProcessAction(ActionBuilder.TrainUnit(unitType, Tag));

        var targetName = GameData.GetUnitTypeData(unitType).Name;
        Logger.Info("{0} started training {1}", Name, targetName);
    }

    public void UpgradeInto(uint unitOrBuildingType) {
        // You upgrade a unit or building by training the upgrade from the producer
        ProcessAction(ActionBuilder.TrainUnit(unitOrBuildingType, Tag));

        var upgradeName = GameData.GetUnitTypeData(unitOrBuildingType).Name;
        Logger.Info("Upgrading {0} into {1}", Name, upgradeName);
    }

    public void PlaceBuilding(uint buildingType, Vector3 target) {
        ProcessAction(ActionBuilder.PlaceBuilding(buildingType, Tag, target));

        var buildingName = GameData.GetUnitTypeData(buildingType).Name;
        Logger.Info("{0} started building {1} at [{2}, {3}]", Name, buildingName, target.X, target.Y);
    }

    public void PlaceExtractor(uint buildingType, Unit gas)
    {
        ProcessAction(ActionBuilder.PlaceExtractor(buildingType, Tag, gas.Tag));

        var buildingName = GameData.GetUnitTypeData(buildingType).Name;
        Logger.Info("{0} started building {1} on gas at [{2}, {3}]", Name, buildingName, gas.Position.X, gas.Position.Y);
    }

    public void ResearchUpgrade(uint upgradeType)
    {
        ProcessAction(ActionBuilder.ResearchUpgrade(upgradeType, Tag));

        var researchName = GameData.GetUpgradeData(upgradeType).Name;
        Logger.Info("{0} started researching {1}", Name, researchName);
    }

    public void Gather(Unit mineralOrGas) {
        ProcessAction(ActionBuilder.Gather(Tag, mineralOrGas.Tag));
    }

    public void ReturnCargo(Unit @base) {
        ProcessAction(ActionBuilder.ReturnCargo(Tag, @base.Tag));

        Logger.Info("{0} returning cargo to {1} at [{2}, {3}]", Name, @base.Name, @base.Position.X, @base.Position.Y);
    }

    public void UseAbility(int abilityId, Point2D position = null, ulong targetUnitTag = ulong.MaxValue) {
        ProcessAction(ActionBuilder.UnitCommand(abilityId, Tag, position, targetUnitTag));

        var abilityName = GameData.GetAbilityData(abilityId).FriendlyName;
        if (targetUnitTag != ulong.MaxValue) {
            Logger.Info("{0} using ability {1} on {2}", Name, abilityName, Controller.UnitsByTag[targetUnitTag].Name);
        }
        else if (position != null) {
            Logger.Info("{0} using ability {1} at [{2}, {3}]", Name, abilityName, position.X, position.Y);
        }
        else {
            Logger.Info("{0} using ability {1}", Name, abilityName);
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
        _deathWatchers.ForEach(watcher => watcher.ReportUnitDeath(this));

        // Reduce the noise
        if (UnitType != Units.Larva) {
            Logger.Info("{0} died for the greater good!", Name);
        }
    }

    public bool IsDead(ulong atFrame) {
        return atFrame - LastSeen > DeathDelay;
    }
}
