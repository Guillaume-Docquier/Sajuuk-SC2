using System;
using System.Numerics;
using Bot.Wrapper;
using Google.Protobuf.Collections;
using SC2APIProtocol;
using Action = SC2APIProtocol.Action;

namespace Bot;

public class Unit {
    private readonly UnitTypeData _unitTypeData;
    public readonly string Name;
    public readonly ulong Tag;
    public readonly uint UnitType; // TODO GD Does this change when morphing?
    public readonly float HealthMax;
    public readonly float ShieldMax;
    public readonly int Supply;

    private SC2APIProtocol.Unit _original;
    public Alliance Alliance;
    public Vector3 Position;
    public float Health;
    public float Shield;
    public float BuildProgress;
    public RepeatedField<UnitOrder> Orders;
    public bool IsVisible;
    public int IdealWorkerCount;
    public int AssignedWorkers;
    public bool IsCargoFull;

    public int SaturatedWorkerCount => Convert.ToInt32(IdealWorkerCount * 1.5);
    public float Integrity => (_original.Health + _original.Shield) / (_original.HealthMax + _original.ShieldMax);

    public Unit(SC2APIProtocol.Unit unit) {
        _unitTypeData = GameData.GetUnitTypeData(unit.UnitType);

        Name = _unitTypeData.Name;
        Tag = unit.Tag;
        UnitType = unit.UnitType;
        HealthMax = unit.HealthMax;
        ShieldMax = unit.ShieldMax;
        Supply = (int)_unitTypeData.FoodRequired;

        Update(unit);
    }

    public void Update(SC2APIProtocol.Unit unit) {
        _original = unit;

        Alliance = unit.Alliance; // Alliance can probably change if being mind controlled?
        Position = new Vector3(unit.Pos.X, unit.Pos.Y, unit.Pos.Z);
        Health = unit.Health;
        Shield = unit.Shield;
        BuildProgress = unit.BuildProgress;
        Orders = unit.Orders;
        IsVisible = unit.DisplayType == DisplayType.Visible;
        IdealWorkerCount = unit.IdealHarvesters;
        AssignedWorkers = unit.AssignedHarvesters;
        // TODO GD This doesn't work!? Both are always 0
        IsCargoFull = unit.CargoSpaceTaken == unit.CargoSpaceMax;
    }

    public double GetDistance(Unit otherUnit) {
        return Vector3.Distance(Position, otherUnit.Position);
    }

    public double GetDistance(Vector3 location) {
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
        Logger.Info("Started training: {0}", targetName);
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
}
