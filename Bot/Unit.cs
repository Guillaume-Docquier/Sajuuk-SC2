using System.Collections.Generic;
using System.Numerics;
using Google.Protobuf.Collections;
using SC2APIProtocol;

// ReSharper disable MemberCanBePrivate.Global

namespace Bot {
    public class Unit {
        private SC2APIProtocol.Unit _original;
        private UnitTypeData _unitTypeData;

        public string Name;
        public uint UnitType;
        public float Integrity;
        public Vector3 Position;
        public ulong Tag;
        public float BuildProgress;
        public UnitOrder Order;
        public RepeatedField<UnitOrder> Orders;
        public int Supply;
        public bool IsVisible;
        public int IdealWorkers;
        public int AssignedWorkers;

        public Unit(SC2APIProtocol.Unit unit) {
            _original = unit;
            _unitTypeData = Controller.GameData.Units[(int)unit.UnitType];

            Name = _unitTypeData.Name;
            Tag = unit.Tag;
            UnitType = unit.UnitType;
            Position = new Vector3(unit.Pos.X, unit.Pos.Y, unit.Pos.Z);
            Integrity = (unit.Health + unit.Shield) / (unit.HealthMax + unit.ShieldMax);
            BuildProgress = unit.BuildProgress;
            IdealWorkers = unit.IdealHarvesters;
            AssignedWorkers = unit.AssignedHarvesters;

            Order = unit.Orders.Count > 0 ? unit.Orders[0] : new UnitOrder();
            Orders = unit.Orders;
            IsVisible = (unit.DisplayType == DisplayType.Visible);

            Supply = (int)_unitTypeData.FoodRequired;
        }

        public double GetDistance(Unit otherUnit) {
            return Vector3.Distance(Position, otherUnit.Position);
        }

        public double GetDistance(Vector3 location) {
            return Vector3.Distance(Position, location);
        }

        public void Train(uint unitType, bool queue = false) {
            if (!queue && Orders.Count > 0) {
                return;
            }

            var abilityId = Abilities.GetId(unitType);
            var action = Controller.CreateRawUnitCommand(abilityId);
            action.ActionRaw.UnitCommand.UnitTags.Add(Tag);
            Controller.AddAction(action);

            var targetName = Controller.GetUnitName(unitType);
            Logger.Info("Started training: {0}", targetName);
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
            var action = Controller.CreateRawUnitCommand(Abilities.Move);
            action.ActionRaw.UnitCommand.TargetWorldSpacePos = new Point2D
            {
                X = target.X,
                Y = target.Y
            };
            action.ActionRaw.UnitCommand.UnitTags.Add(Tag);
            Controller.AddAction(action);
        }

        public void Smart(Unit unit) {
            var action = Controller.CreateRawUnitCommand(Abilities.Smart);
            action.ActionRaw.UnitCommand.TargetUnitTag = unit.Tag;
            action.ActionRaw.UnitCommand.UnitTags.Add(Tag);
            Controller.AddAction(action);
        }
    }
}
