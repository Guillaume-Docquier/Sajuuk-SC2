using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sajuuk.ExtensionMethods;
using Google.Protobuf.Collections;
using Sajuuk.Actions;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.Managers;
using Sajuuk.MapAnalysis.RegionAnalysis;
using Sajuuk.UnitModules;
using Sajuuk.Utils;
using SC2APIProtocol;
using Action = SC2APIProtocol.Action;

namespace Sajuuk;

public class Unit: ICanDie, IHavePosition {
    private readonly IFrameClock _frameClock;
    private readonly KnowledgeBase _knowledgeBase;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly IUnitsTracker _unitsTracker;

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

    /// <summary>
    /// The number of the first frame where this unit was seen.
    /// For enemy units that reappear this will be the frame number of when they reappeared (because it's harder to implement otherwise).
    /// </summary>
    public ulong FirstSeen;

    /// <summary>
    /// The number of the last frame where this unit was seen.
    /// </summary>
    public ulong LastSeen;

    public HashSet<uint> Buffs;

    public int InitialMineralCount = int.MaxValue;
    public int InitialVespeneCount = int.MaxValue;

    /// <summary>
    /// Whether this unit can hit air units
    /// </summary>
    public bool CanHitAir;
    /// <summary>
    /// Whether this unit can hit ground units
    /// </summary>
    public bool CanHitGround;

    public bool IsFlying => RawUnitData.IsFlying;
    public bool IsBurrowed => RawUnitData.IsBurrowed;
    public bool IsCloaked => RawUnitData.Cloak == CloakState.Cloaked;

    /// <summary>
    /// The currently engaged target, or null if none.
    /// </summary>
    public Unit EngagedTarget => _unitsTracker.UnitsByTag.TryGetValue(RawUnitData.EngagedTargetTag, out var engagedTarget) ? engagedTarget : null;
    /// <summary>
    /// Whether the unit has a target.
    /// False does not mean that an enemy is not engaging the unit, it only means that the unit is not currently fighting back.
    /// </summary>
    public bool IsEngagingTheEnemy => EngagedTarget != null;
    /// <summary>
    /// Whether the unit has a target and is in range of the target.
    /// False does not mean that an enemy is not engaging the unit, it only means that the unit is not currently fighting back.
    /// </summary>
    public bool IsFightingTheEnemy => IsEngagingTheEnemy && IsInAttackRangeOf(EngagedTarget);

    /// <summary>
    /// Whether the unit has offensive weapons.
    /// </summary>
    public bool HasWeapons;
    /// <summary>
    /// Represents the % of cooldown remaining.
    /// 0% means the unit can attack.
    /// </summary>
    public double WeaponCooldownPercent;
    private double _maxWeaponCooldownFrames;
    /// <summary>
    /// Whether the unit can use an attack right now.
    /// </summary>
    public bool IsReadyToAttack;

    /// <summary>
    /// The angle where the unit is facing, in radians.
    /// 0deg is looking to the left, 90deg is looking up.
    /// </summary>
    public float Facing => RawUnitData.Facing;

    public float MaxRange { get; private set; }

    public ulong DeathDelay = 0;

    public List<OrderAction> Actions { get; private set; }

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

    /// <summary>
    /// The current health + shields
    /// </summary>
    public float HitPoints;
    /// <summary>
    /// The max health + shields
    /// </summary>
    public float MaxHitPoints;
    /// <summary>
    /// The % of remaining hit points.
    /// 0% means the unit is dead.
    /// </summary>
    public float Integrity;

    /// <summary>
    /// Whether the unit is completely built.
    /// </summary>
    public bool IsOperational => _buildProgress >= 1;

    /// <summary>
    /// Units inside extractors are not available. We keep them in memory but they're not in the game for some time.
    /// Same might be true for units inside transports, but I haven't tested it yet.
    /// </summary>
    public bool IsAvailable => LastSeen >= _frameClock.CurrentFrame;

    public IEnumerable<UnitOrder> OrdersExceptMining => Orders.Where(order => order.AbilityId != Abilities.Move
                                                                              && !Abilities.Gather.Contains(order.AbilityId)
                                                                              && !Abilities.ReturnCargo.Contains(order.AbilityId));

    // TODO GD I don't know if I like needing a unitsTracker here. Maybe the logic should be extracted out.
    public Unit(
        IFrameClock frameClock,
        KnowledgeBase knowledgeBase,
        ITerrainTracker terrainTracker,
        IRegionsTracker regionsTracker,
        IUnitsTracker unitsTracker,
        SC2APIProtocol.Unit rawUnit,
        ulong currentFrame
    ) {
        _frameClock = frameClock;
        _knowledgeBase = knowledgeBase;
        _terrainTracker = terrainTracker;
        _regionsTracker = regionsTracker;
        _unitsTracker = unitsTracker;

        FirstSeen = currentFrame;
        Actions = new List<OrderAction>();

        Update(rawUnit, currentFrame);
    }

    public void Update(SC2APIProtocol.Unit rawUnit, ulong currentFrame) {
        var unitTypeChanged = rawUnit.UnitType != UnitType;

        RawUnitData = rawUnit;
        _buildProgress = rawUnit.BuildProgress;

        Tag = rawUnit.Tag;
        UnitType = rawUnit.UnitType;
        Radius = rawUnit.Radius;
        Alliance = rawUnit.Alliance;
        Position = rawUnit.Pos.ToVector3();
        Orders = rawUnit.Orders;
        IsVisible = rawUnit.DisplayType == DisplayType.Visible; // TODO GD This is not actually visible as in cloaked
        LastSeen = currentFrame;
        Buffs = new HashSet<uint>(rawUnit.BuffIds);

        if (unitTypeChanged) {
            UnitTypeData = _knowledgeBase.GetUnitTypeData(rawUnit.UnitType);
            Name = UnitTypeData.Name;
            FoodRequired = UnitTypeData.FoodRequired;

            AliasUnitTypeData = UnitTypeData.HasUnitAlias ? _knowledgeBase.GetUnitTypeData(UnitTypeData.UnitAlias) : null;

            UpdateWeaponsData(UnitTypeData.Weapons.ToList());
        }

        WeaponCooldownPercent = HasWeapons
            ? RawUnitData.WeaponCooldown / _maxWeaponCooldownFrames
            : 1;

        IsReadyToAttack = WeaponCooldownPercent <= 0;

        // It looks like snapshotted units don't have HP data
        // We won't update in that case, so that we remember what was last seen
        if (RawUnitData.HealthMax + RawUnitData.ShieldMax > 0) {
            HitPoints = RawUnitData.Health + RawUnitData.Shield;
            MaxHitPoints = RawUnitData.HealthMax + RawUnitData.ShieldMax;
            Integrity = HitPoints / MaxHitPoints;
        }

        // Snapshot minerals/gas don't have contents
        if (IsVisible && InitialMineralCount == int.MaxValue) {
            InitialMineralCount = RawUnitData.MineralContents;
            InitialVespeneCount = RawUnitData.VespeneContents;
        }
    }

    private void UpdateWeaponsData(IReadOnlyList<Weapon> weapons) {
        HasWeapons = weapons.Count > 0;
        MaxRange = HasWeapons
            ? weapons.Max(weapon => weapon.Range)
            : 0;

        // Weapon speed is in seconds between attacks
        _maxWeaponCooldownFrames = HasWeapons
            // TODO GD Not sure how to handle multiple weapons
            ? weapons[0].Speed * TimeUtils.FramesPerSecond
            : float.MaxValue;

        CanHitAir = IsOperational && weapons.Any(weapon => weapon.Type is Weapon.Types.TargetType.Any or Weapon.Types.TargetType.Air);
        CanHitGround = IsOperational && weapons.Any(weapon => weapon.Type is Weapon.Types.TargetType.Any or Weapon.Types.TargetType.Ground);
    }

    public double DistanceTo(Unit otherUnit) {
        return DistanceTo(otherUnit.Position.ToVector2());
    }

    public double DistanceTo(Vector3 location) {
        return DistanceTo(location.ToVector2());
    }

    public double DistanceTo(Vector2 location) {
        return Position.ToVector2().DistanceTo(location);
    }

    public IRegion GetRegion() {
        // TODO GD I'm not convinced I want to inject stuff into unit, we'll have to revisit that
        var unitRegion = _regionsTracker.GetRegion(Position);
        if (unitRegion != null) {
            return unitRegion;
        }

        // TODO GD Injecting TerrainTracker here means a circular dependency between the UnitsTracker and TerrainTracker
        return _terrainTracker.BuildSearchGrid(Position, gridRadius: 3)
            .Where(cell => _terrainTracker.IsWalkable(cell))
            .Select(cell => _regionsTracker.GetRegion(cell))
            .FirstOrDefault(region => region != null);
    }

    // TODO GD Make sure to cancel any other order and prevent orders to be added for this frame
    public void Stop() {
        if (Orders.Any()) {
            ProcessAction(Abilities.Stop);
        }
    }

    /// <summary>
    /// Move in the direction of the provided vector
    /// If you want to move by a certain distance, provide a direction vector of that length
    /// </summary>
    /// <param name="direction"></param>
    public void MoveInDirection(Vector2 direction) {
        Move(Vector2.Add(Position.ToVector2(), direction));
    }

    /// <summary>
    /// Send the order to move towards a target position by a certain distance
    /// </summary>
    /// <param name="target">The target position to move towards</param>
    /// <param name="distance">The step distance</param>
    public void MoveTowards(Vector2 target, float distance = 0.5f) {
        Move(Position.ToVector2().TranslateTowards(target, distance), distance / 2);
    }

    /// <summary>
    /// Send the order to move away from a target position by a certain distance
    /// </summary>
    /// <param name="target">The target position to move away from</param>
    /// <param name="distance">The step distance</param>
    public void MoveAwayFrom(Vector2 target, float distance = 1f) {
        Move(Position.ToVector2().TranslateAwayFrom(target, distance), distance / 2);
    }

    /// <summary>
    /// Send the order to move to a target position.
    /// If the unit already has a move order to that target, given the precision, no order will be sent.
    /// You can override this check with allowSpam = true.
    /// </summary>
    /// <param name="target">The target position to move to</param>
    /// <param name="precision">The allowed precision on the move order</param>
    /// <param name="allowSpam">Enables spamming orders. Not recommended because it might generate a lot of actions</param>
    public void Move(Vector2 target, float precision = 0.5f, bool allowSpam = false) {
        if (Position.ToVector2().DistanceTo(target) <= 0.01) {
            return;
        }

        if (!allowSpam && IsTargeting(target, Abilities.Move, precision)) {
            return;
        }

        ProcessAction(Abilities.Move,target);
    }

    /// <summary>
    /// Orders this unit to attack a specific unit.
    /// If the unit is already attacking the specified unit, no additional order is sent
    /// </summary>
    /// <param name="targetUnit">The target unit to attack</param>
    public void Attack(Unit targetUnit) {
        // TODO GD Should check if can move while burrowed
        if (IsBurrowed) {
            Move(targetUnit.Position.ToVector2());
            return;
        }

        if (IsAttacking(targetUnit)) {
            return;
        }

        //ProcessAction(_actionBuilder.Attack(Tag, targetUnit.Tag));
        ProcessAction(Abilities.Attack, targetUnit.Tag);
    }

    /// <summary>
    /// Send the order to attack move to a target position.
    /// If the unit already has an attack move order to that target, given the precision, no order will be sent.
    /// You can override this check with allowSpam = true.
    /// </summary>
    /// <param name="target">The target position to attack move to</param>
    /// <param name="precision">The allowed precision on the attack move order</param>
    /// <param name="allowSpam">Enables spamming orders. Not recommended because it might generate a lot of actions</param>
    public void AttackMove(Vector2 target, float precision = 0.5f, bool allowSpam = false) {
        // TODO GD Should check if can move while burrowed
        if (IsBurrowed) {
            Move(target, precision, allowSpam);
            return;
        }

        if (!allowSpam && IsTargeting(target, Abilities.Attack, precision)) {
            return;
        }

        ProcessAction(Abilities.Attack, target);
    }

    public void TrainUnit(uint unitType, bool queue = false) {
        // TODO GD This should be handled when choosing a producer
        if (!queue && Orders.Count > 0) {
            var nameOfUnitToTrain = _knowledgeBase.GetUnitTypeData(unitType).Name;
            var orders = string.Join(",", Orders.Select(order => order.AbilityId));
            Logger.Error("A {0} is trying to train {1}, but it already has the orders {2} and queue is false", this, nameOfUnitToTrain, orders);

            return;
        }

        var unitAbilityId = _knowledgeBase.GetUnitTypeData(unitType).AbilityId;
        ProcessAction(unitAbilityId);

        var targetName = _knowledgeBase.GetUnitTypeData(unitType).Name;
        Logger.Info("{0} started training {1}", this, targetName);
    }

    public void UpgradeInto(uint unitOrBuildingType) {
        // You upgrade a unit or building by training the upgrade from the producer
        var unitAbilityId = _knowledgeBase.GetUnitTypeData(unitOrBuildingType).AbilityId;
        ProcessAction(unitAbilityId);

        var upgradeName = _knowledgeBase.GetUnitTypeData(unitOrBuildingType).Name;
        Logger.Info("Upgrading {0} into {1}", this, upgradeName);
    }

    public void PlaceBuilding(uint buildingType, Vector2 target) {
        Manager?.Release(this);
        var buildingAbilityId = _knowledgeBase.GetUnitTypeData(buildingType).AbilityId;
        ProcessAction(buildingAbilityId, target);

        var buildingName = _knowledgeBase.GetUnitTypeData(buildingType).Name;
        Logger.Info("{0} started building {1} at {2}", this, buildingName, target);
    }

    public void PlaceExtractor(uint buildingType, Unit gas) {
        Manager?.Release(this);
        var buildingAbilityId = _knowledgeBase.GetUnitTypeData(buildingType).AbilityId;
        ProcessAction(buildingAbilityId,gas.Tag);

        var buildingName = _knowledgeBase.GetUnitTypeData(buildingType).Name;
        Logger.Info("{0} started building {1} on gas at {2}", this, buildingName, gas.Position);
    }

    public void ResearchUpgrade(uint upgradeType)
    {
        var upgradeAbilityId = _knowledgeBase.GetUpgradeData(upgradeType).AbilityId;
        ProcessAction(upgradeAbilityId);

        var researchName = _knowledgeBase.GetUpgradeData(upgradeType).Name;
        Logger.Info("{0} started researching {1}", this, researchName);
    }

    public void Gather(Unit mineralOrGas) {
        ProcessAction(Abilities.HarvestGather, mineralOrGas.Tag);
    }

    public void ReturnCargo() {
        ProcessAction(Abilities.HarvestReturn);
    }

    public void UseAbility(uint abilityId, Point2D position = null, ulong targetUnitTag = ulong.MaxValue) {
        if (Orders.Any(order => order.AbilityId == abilityId)) {
            return;
        }

        if (Abilities.EnergyCost.TryGetValue(abilityId, out var energyCost) && RawUnitData.Energy < energyCost) {
            RawUnitData.Energy -= energyCost;
        }

        if(targetUnitTag != ulong.MaxValue)
            ProcessAction(abilityId, targetUnitTag);
        else if(position != null)
            ProcessAction(abilityId, position.ToVector2());
        else
            ProcessAction(abilityId);

        var abilityName = _knowledgeBase.GetAbilityData(abilityId).FriendlyName;
        if (targetUnitTag != ulong.MaxValue) {
            if (!_unitsTracker.UnitsByTag.ContainsKey(targetUnitTag)) {
                Logger.Error("Error with {0} trying to {1} on {2}: The target doesn't exist", this, abilityName, Tag);
                return;
            }

            if (abilityId != Abilities.Smart) {
                var targetUnit = _unitsTracker.UnitsByTag[targetUnitTag];
                Logger.Info("{0} using ability {1} on {2}", this, abilityName, targetUnit);
            }
        }
        else if (position != null) {
            Logger.Info("{0} using ability {1} at {2}", this, abilityName, position.ToVector2());
        }
        else {
            Logger.Info("{0} using ability {1}", this, abilityName);
        }
    }

    private void ProcessAction(uint abilityId ,Vector2 position)
    {
        Actions.Add(new OrderAction()
        {
            AbilityId = abilityId,
            TargetPosition = position,
        });

        var order = new UnitOrder
        {
            AbilityId = (uint)abilityId,
            TargetWorldSpacePos = new Point {
                X = position.X,
                Y = position.Y,
                Z = 0, // We don't know
            },
        };

        Orders.Add(order);
    }

    private void ProcessAction(uint abilityId, ulong targetUnitTag)
    {
        Actions.Add(new OrderAction()
        {
            AbilityId = abilityId,
            TargetUnit=targetUnitTag
        });

        var order = new UnitOrder
        {
            AbilityId = (uint)abilityId,
            TargetUnitTag = targetUnitTag,
        };

        Orders.Add(order);
    }

    private void ProcessAction(uint abilityId)
    {
        Actions.Add(new OrderAction()
        {
            AbilityId = abilityId
        });

        var order = new UnitOrder
        {
            AbilityId = (uint)abilityId,
        };

        Orders.Add(order);
    }

    public void AddDeathWatcher(IWatchUnitsDie watcher) {
        DeathWatchers.Add(watcher);
    }

    public void RemoveDeathWatcher(IWatchUnitsDie watcher) {
        DeathWatchers.Remove(watcher);
    }

    public void Died() {
        Logger.Info($"{this} died");

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

    /// <summary>
    /// Checks if an ability already targets a target position given a certain level of precision.
    /// </summary>
    /// <param name="target">The target to check.</param>
    /// <param name="abilityId">The ability to check.</param>
    /// <param name="precision">The maximum distance allowed between target and order.TargetWorldSpacePos. Should be positive.</param>
    /// <returns>True if there is an order with abilityId where the TargetWorldSpacePos is within precision of the target.</returns>
    private bool IsTargeting(Vector2 target, uint abilityId, float precision) {
        return Orders
            .Where(order => order.AbilityId == abilityId)
            .Where(order => order.TargetWorldSpacePos != null)
            .Any(order => order.TargetWorldSpacePos.ToVector2().DistanceTo(target) <= precision);
    }

    private bool IsAttacking(Unit unit) {
        return Orders.Any(order => order.TargetUnitTag == unit.Tag);
    }

    // TODO GD This doesn't work with upgrades
    public bool IsProducing(uint buildingOrUnitType, Vector2 atLocation = default) {
        var buildingAbilityId = _knowledgeBase.GetUnitTypeData(buildingOrUnitType).AbilityId;

        var producingOrder = Orders.FirstOrDefault(order => order.AbilityId == buildingAbilityId);

        if (producingOrder == null) {
            return false;
        }

        if (atLocation == default) {
            return true;
        }

        if (producingOrder.TargetWorldSpacePos != null) {
            // TargetWorldSpacePos is a point, but never has a Z
            return producingOrder.TargetWorldSpacePos.Equals(new Point { X = atLocation.X, Y = atLocation.Y, Z = 0 });
        }

        // Extractors are built on a gas, not at a location
        if (_unitsTracker.UnitsByTag.TryGetValue(producingOrder.TargetUnitTag, out var targetUnit)) {
            return targetUnit.Position.ToVector2() == atLocation;
        }

        return false;
    }

    /// <summary>
    /// Whether the unit has the capacity to attack the other unit based on its weapon types and if the other unit is flying or grounded.
    /// </summary>
    /// <param name="otherUnit">The unit to attack</param>
    /// <returns>True if the unit has the proper weapons to attack the other unit.</returns>
    public bool CanAttack(Unit otherUnit) {
        if (otherUnit.IsCloaked) {
            return false;
        }

        if (otherUnit.IsFlying) {
            return CanHitAir;
        }

        return CanHitGround;
    }

    /// <summary>
    /// Determines if the unit is in attack range of another unit based on their hit boxes and distance.
    /// </summary>
    /// <param name="otherUnit">The unit to attack</param>
    /// <returns>True if the unit is in attack range of the other unit</returns>
    public bool IsInAttackRangeOf(Unit otherUnit) {
        return DistanceTo(otherUnit) <= otherUnit.Radius + MaxRange + Radius;
    }

    /// <summary>
    /// Determines if the unit is in attack range of another unit based on their hit boxes and distance.
    /// If you can, you should use IsInAttackRangeOf(Unit) instead because considering the other unit's hit box can make a big difference.
    /// </summary>
    /// <param name="position">The position to attack</param>
    /// <returns>True if the unit is in attack range of the other unit</returns>
    public bool IsInAttackRangeOf(Vector2 position) {
        return DistanceTo(position) <= MaxRange + Radius;
    }

    /// <summary>
    /// Determines if the unit is in sight range of another unit based on their hit boxes and distance.
    /// </summary>
    /// <param name="otherUnit">The unit to see</param>
    /// <returns>True if the unit is in sight range of the other unit</returns>
    public bool IsInSightRangeOf(Unit otherUnit) {
        return DistanceTo(otherUnit) <= otherUnit.Radius + UnitTypeData.SightRange + Radius;
    }

    /// <summary>
    /// Determines if the unit is in attack range of another unit based on their hit boxes and distance.
    /// If you can, you should use IsInSightRangeOf(Unit) instead because considering the other unit's hit box can make a big difference.
    /// </summary>
    /// <param name="position">The position to attack</param>
    /// <returns>True if the unit is in sight range of the other unit</returns>
    public bool IsInSightRangeOf(Vector2 position) {
        return DistanceTo(position) <= UnitTypeData.SightRange + Radius;
    }

    public bool HasOrders() {
        return Orders.Count > 0;
    }

    public bool IsIdle() {
        return !Orders.Any();
    }

    public bool IsMoving() {
        return Orders.Any(order => order.AbilityId == Abilities.Move);
    }

    public bool IsAttacking() {
        return Orders.Any(order => order.AbilityId == Abilities.Attack);
    }

    public bool IsMineralWalking() {
        return Orders.Any(order => Abilities.Gather.Contains(order.AbilityId));
    }

    public override string ToString() {
        if (Alliance == Alliance.Self) {
            return $"{Name}[{Tag}]";
        }

        return $"{Alliance} {Name}[{Tag}]";
    }
}
