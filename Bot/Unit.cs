using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.Wrapper;
using Google.Protobuf.Collections;
using SC2APIProtocol;

namespace Bot;

public class Unit {
    private SC2APIProtocol.Unit _original;
    private UnitTypeData _unitTypeData;

    public string Name;
    public uint UnitType;
    public float Integrity;
    public Vector3 Position;
    public ulong Tag;
    public float BuildProgress;
    public RepeatedField<UnitOrder> Orders;
    public int Supply;
    public bool IsVisible;
    public int IdealWorkers;
    public int AssignedWorkers;

    public Unit(SC2APIProtocol.Unit unit) {
        _original = unit;
        _unitTypeData = GameData.GetUnitTypeData(unit.UnitType);

        Name = _unitTypeData.Name;
        Tag = unit.Tag;
        UnitType = unit.UnitType;
        Position = new Vector3(unit.Pos.X, unit.Pos.Y, unit.Pos.Z);
        Integrity = (unit.Health + unit.Shield) / (unit.HealthMax + unit.ShieldMax);
        BuildProgress = unit.BuildProgress;
        IdealWorkers = unit.IdealHarvesters;
        AssignedWorkers = unit.AssignedHarvesters;

        Orders = unit.Orders;
        IsVisible = unit.DisplayType == DisplayType.Visible;

        Supply = (int)_unitTypeData.FoodRequired;
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

    public void ResearchTech(int techAbilityId)
    {
        ProcessAction(ActionBuilder.ResearchTech(techAbilityId, Tag));

        var unitTypeData = GameData.GetUnitTypeData((uint)techAbilityId);
        var researchTypeData = GameData.GetResearchData((uint)techAbilityId);
        var researchName = GameData.GetResearchData((uint)techAbilityId).Name;
        Logger.Info("{0} started researching {1}", Name, researchName);
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
