using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.Managers;
using Bot.MapKnowledge;
using Bot.UnitModules;
using Bot.Wrapper;
using Google.Protobuf.Collections;
using SC2APIProtocol;
using Action = SC2APIProtocol.Action;

namespace Bot;

public class Unit: ICanDie, IHavePosition {
    public readonly HashSet<IWatchUnitsDie> DeathWatchers = new HashSet<IWatchUnitsDie>();
    public UnitTypeData UnitTypeData;

    // An alias unit type happens when the same unit has different modes.
    // For example, burrowed units alias is the un-burrowed version of the unit.
    // This is also probably true with flying terran structures and siege tanks.
    public UnitTypeData AliasUnitTypeData;

    public string Name;
    public ulong Tag;
    public uint UnitType;
    public float FoodRequired;
    public float Radius;
    public SC2APIProtocol.Unit RawUnitData;
    public Alliance Alliance;
    public Vector3 Position { get; private set; }
    private float _buildProgress;
    public RepeatedField<UnitOrder> Orders;
    public bool IsVisible;
    public ulong LastSeen;
    public HashSet<uint> Buffs;
    public int InitialMineralCount = int.MaxValue;
    public int InitialVespeneCount = int.MaxValue;

    public bool CanHitAir => UnitTypeData.Weapons.Any(weapon => weapon.Type is Weapon.Types.TargetType.Any or Weapon.Types.TargetType.Air);
    public bool CanHitGround => UnitTypeData.Weapons.Any(weapon => weapon.Type is Weapon.Types.TargetType.Any or Weapon.Types.TargetType.Ground);
    public bool IsFlying => RawUnitData.IsFlying;
    public bool IsBurrowed => RawUnitData.IsBurrowed;
    public bool IsCloaked => RawUnitData.Cloak == CloakState.Cloaked;

    public float MaxRange { get; private set; }

    public ulong DeathDelay = 0;

    private Manager _manager;
    public Manager Manager {
        get => _manager;
        set {
            if (_manager == value) {
                return;
            }

            if (value != null) {
                _manager?.Release(this);
            }

            _manager = value;
        }
    }

    private Supervisor _supervisor;
    public Supervisor Supervisor {
        get => _supervisor;
        set {
            if (_supervisor == value) {
                return;
            }

            if (value != null) {
                _supervisor?.Release(this);
            }

            _supervisor = value;
        }
    }

    public readonly Dictionary<string, IUnitModule> Modules = new Dictionary<string, IUnitModule>();

    public float Integrity => (RawUnitData.Health + RawUnitData.Shield) / (RawUnitData.HealthMax + RawUnitData.ShieldMax);

    public bool IsOperational => _buildProgress >= 1;

    // Units inside extractors are not available. We keep them in memory but they're not in the game for some time
    public bool IsAvailable => LastSeen >= Controller.Frame;

    public IEnumerable<UnitOrder> OrdersExceptMining => Orders.Where(order => order.AbilityId != Abilities.Move
                                                                              && !Abilities.Gather.Contains(order.AbilityId)
                                                                              && !Abilities.ReturnCargo.Contains(order.AbilityId));

    public Unit(SC2APIProtocol.Unit unit, ulong frame) {
        Update(unit, frame);
    }

    public void Update(SC2APIProtocol.Unit unit, ulong frame) {
        var unitTypeChanged = unit.UnitType != UnitType;

        RawUnitData = unit;

        if (unitTypeChanged) {
            UnitTypeData = KnowledgeBase.GetUnitTypeData(unit.UnitType);
            AliasUnitTypeData = UnitTypeData.HasUnitAlias ? KnowledgeBase.GetUnitTypeData(UnitTypeData.UnitAlias) : null;

            var weapons = UnitTypeData.Weapons.Concat(AliasUnitTypeData?.Weapons ?? Enumerable.Empty<Weapon>()).ToList();
            MaxRange = weapons.Count <= 0 ? 0 : weapons.Max(weapon => weapon.Range);
        }

        Name = UnitTypeData.Name;
        Tag = unit.Tag;
        UnitType = unit.UnitType;
        FoodRequired = UnitTypeData.FoodRequired;
        Radius = unit.Radius;
        Alliance = unit.Alliance;
        Position = unit.Pos.ToVector3();
        _buildProgress = unit.BuildProgress;
        Orders = unit.Orders;
        IsVisible = unit.DisplayType == DisplayType.Visible; // TODO GD This is not actually visible as in cloaked
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

    public double HorizontalDistanceTo(Vector2 location) {
        return Position.ToVector2().DistanceTo(location);
    }

    public Region GetRegion() {
        return Position.GetRegion();
    }

    // TODO GD Make sure to cancel any other order and prevent orders to be added for this frame
    public void Stop() {
        if (Orders.Any()) {
            ProcessAction(ActionBuilder.Stop(Tag));
        }
    }

    /// <summary>
    /// Send the order to move towards a target position by a certain distance
    /// </summary>
    /// <param name="target">The target position to move towards</param>
    /// <param name="distance">The step distance</param>
    public void MoveTowards(Vector3 target, float distance = 0.5f) {
        Move(Position.TranslateTowards(target, distance), distance / 2);
    }

    /// <summary>
    /// Send the order to move to a target position.
    /// If the unit already has a move order to that target, given the precision, no order will be sent.
    /// You can override this check with allowSpam = true.
    /// </summary>
    /// <param name="target">The target position to move to</param>
    /// <param name="precision">The allowed precision on the move order</param>
    /// <param name="allowSpam">Enables spamming orders. Not recommended because it might generate a lot of actions</param>
    public void Move(Vector3 target, float precision = 0.5f, bool allowSpam = false) {
        if (Position.HorizontalDistanceTo(target) <= 0.01) {
            return;
        }

        if (!allowSpam && IsTargeting(target, Abilities.Move, precision)) {
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

    /// <summary>
    /// Send the order to attack move to a target position.
    /// If the unit already has an attack move order to that target, given the precision, no order will be sent.
    /// You can override this check with allowSpam = true.
    /// </summary>
    /// <param name="target">The target position to attack move to</param>
    /// <param name="precision">The allowed precision on the attack move order</param>
    /// <param name="allowSpam">Enables spamming orders. Not recommended because it might generate a lot of actions</param>
    public void AttackMove(Vector3 target, float precision = 0.5f, bool allowSpam = false) {
        if (!allowSpam && IsTargeting(target, Abilities.Attack, precision)) {
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
            var nameOfUnitToTrain = KnowledgeBase.GetUnitTypeData(unitType).Name;
            var orders = string.Join(",", Orders.Select(order => order.AbilityId));
            Logger.Error("A {0} is trying to train {1}, but it already has the orders {2} and queue is false", this, nameOfUnitToTrain, orders);

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
        Manager?.Release(this);
        ProcessAction(ActionBuilder.PlaceBuilding(buildingType, Tag, target));

        var buildingName = KnowledgeBase.GetUnitTypeData(buildingType).Name;
        Logger.Info("{0} started building {1} at {2}", this, buildingName, target);
    }

    public void PlaceExtractor(uint buildingType, Unit gas) {
        Manager?.Release(this);
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
        DeathWatchers.Add(watcher);
    }

    public void RemoveDeathWatcher(IWatchUnitsDie watcher) {
        DeathWatchers.Remove(watcher);
    }

    public void Died() {
        // Reduce the noise
        if (UnitType is not Units.Larva and not Units.Egg) {
            Logger.Info("{0} died", this);
        }

        // We .ToList() to make a copy of _deathWatchers because some ReportUnitDeath might call RemoveDeathWatcher
        // They shouldn't, partly because it modifies the collection while we are iterating it
        // But mostly because the unit will only die once
        DeathWatchers.ToList().ForEach(watcher => watcher.ReportUnitDeath(this));
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

    /// <summary>
    /// Checks if an ability already targets a target position given a certain level of precision.
    /// </summary>
    /// <param name="target">The target to check.</param>
    /// <param name="abilityId">The ability to check.</param>
    /// <param name="precision">The maximum distance allowed between target and order.TargetWorldSpacePos. Should be positive.</param>
    /// <returns>True if there is an order with abilityId where the TargetWorldSpacePos is within precision of the target.</returns>
    private bool IsTargeting(Vector3 target, uint abilityId, float precision) {
        return Orders
            .Where(order => order.AbilityId == abilityId)
            .Where(order => order.TargetWorldSpacePos != null)
            .Any(order => order.TargetWorldSpacePos.ToVector3().HorizontalDistanceTo(target) <= precision);
    }

    private bool IsAttacking(Unit unit) {
        return Orders.Any(order => order.TargetUnitTag == unit.Tag);
    }

    public bool IsEngaging(IReadOnlySet<ulong> unitTags) {
        return unitTags.Contains(RawUnitData.EngagedTargetTag);
    }

    // TODO GD This doesn't work with upgrades
    public bool IsProducing(uint buildingOrUnitType, Vector3 atLocation = default) {
        var buildingAbilityId = KnowledgeBase.GetUnitTypeData(buildingOrUnitType).AbilityId;

        var producingOrder = Orders.FirstOrDefault(order => order.AbilityId == buildingAbilityId);

        if (producingOrder == null) {
            return false;
        }

        if (atLocation == default) {
            return true;
        }

        if (producingOrder.TargetWorldSpacePos != null) {
            return producingOrder.TargetWorldSpacePos.Equals(atLocation.WithoutZ().ToPoint());
        }

        // Extractors are built on a gas, not at a location
        if (UnitsTracker.UnitsByTag.TryGetValue(producingOrder.TargetUnitTag, out var targetUnit)) {
            return targetUnit.Position.WithoutZ() == atLocation.WithoutZ();
        }

        return false;
    }

    public bool IsInAttackRangeOf(Unit enemy) {
        return IsInAttackRangeOf(enemy.Position);
    }

    public bool IsInAttackRangeOf(Vector3 position) {
        return HorizontalDistanceTo(position) <= MaxRange;
    }

    public bool IsInSightRangeOf(Unit enemy) {
        return IsInAttackRangeOf(enemy.Position);
    }

    public bool IsInSightRangeOf(Vector3 position) {
        return HorizontalDistanceTo(position) <= UnitTypeData.SightRange;
    }

    public override string ToString() {
        if (Alliance == Alliance.Self) {
            return $"{Name}[{Tag}]";
        }

        return $"{Alliance} {Name}[{Tag}]";
    }
}
