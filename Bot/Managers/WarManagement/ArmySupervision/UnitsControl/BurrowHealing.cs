using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.GameSense.RegionTracking;
using SC2APIProtocol;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl;

public class BurrowHealing : IUnitsControl {
    private readonly IUnitsTracker _unitsTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly IRegionsEvaluationsTracker _regionsEvaluationsTracker;

    private const double BurrowDownThreshold = 0.5;
    private const double BurrowUpThreshold = 0.6;

    public BurrowHealing(IUnitsTracker unitsTracker, ITerrainTracker terrainTracker, IRegionsTracker regionsTracker, IRegionsEvaluationsTracker regionsEvaluationsTracker) {
        _unitsTracker = unitsTracker;
        _terrainTracker = terrainTracker;
        _regionsTracker = regionsTracker;
        _regionsEvaluationsTracker = regionsEvaluationsTracker;
    }

    public bool IsExecuting() {
        // TODO GD Should we track who we burrowed?
        return false;
    }

    // TODO GD We could now consider mass resurface (burrow unity) instead of resurfacing 1 by 1
    // TODO GD We could split this IUnitsControl into 3 distinct ones (Burrow, resurface, tunnel)
    // TODO GD BurrowHealing would execute the 3 sub-controls
    public IReadOnlySet<Unit> Execute(IReadOnlySet<Unit> army) {
        if (!Controller.ResearchedUpgrades.Contains(Upgrades.Burrow)) {
            return army;
        }

        var uncontrolledUnits = new HashSet<Unit>(army);
        var roaches = Controller.GetUnits(army, Units.Roach).ToList();

        // Burrow
        var roachesThatNeedBurrowing = roaches
            .Where(roach => !roach.IsBurrowed)
            .Where(roach => roach.Integrity <= BurrowDownThreshold)
            .Where(roach => !DetectionTracker.Instance.IsDetected(roach))
            .Where(roach => !GetCollidingUnits(roach, checkUnderground: true).Any());

        foreach (var roach in roachesThatNeedBurrowing) {
            roach.UseAbility(Abilities.BurrowRoachDown);
            uncontrolledUnits.Remove(roach);
        }

        var canTunnel = Controller.ResearchedUpgrades.Contains(Upgrades.TunnelingClaws);

        // Resurface
        var roachesThatNeedResurfacing = roaches
            .Where(roach => roach.IsBurrowed)
            .Where(roach => roach.Integrity >= BurrowUpThreshold || DetectionTracker.Instance.IsDetected(roach));

        foreach (var roach in roachesThatNeedResurfacing) {
            Resurface(roach, canTunnel);
            uncontrolledUnits.Remove(roach);
        }

        // Move underground
        if (canTunnel) {
            var roachesThatNeedToTunnel = roaches
                .Where(roach => roach.IsBurrowed)
                .Where(roach => roach.Integrity <= BurrowUpThreshold)
                .Where(roach => !DetectionTracker.Instance.IsDetected(roach));

            foreach (var roach in roachesThatNeedToTunnel) {
                if (TunnelToSafety(roach)) {
                    uncontrolledUnits.Remove(roach);
                }
            }
        }

        return uncontrolledUnits;
    }

    // TODO GD Is this necessary? Must we ensure that all roaches properly resurface?
    public void Reset(IReadOnlyCollection<Unit> army) {
        var canTunnel = Controller.ResearchedUpgrades.Contains(Upgrades.TunnelingClaws);
        var burrowedRoaches = Controller.GetUnits(army, Units.Roach).Where(roach => roach.IsBurrowed);

        foreach (var burrowedRoach in burrowedRoaches) {
            Resurface(burrowedRoach, canTunnel);
        }
    }

    /// <summary>
    /// Resurface the given roach if there is not another unit above it.
    /// If there is a unit above and we can tunnel, try to move away from the colliding unit.
    /// </summary>
    /// <param name="roach">The roach to resurface.</param>
    /// <param name="canTunnel">Whether we can move while underground.</param>
    private void Resurface(Unit roach, bool canTunnel) {
        var collidingUnits = GetCollidingUnits(roach, checkUnderground: false).ToList();
        if (collidingUnits.Count <= 0) {
            roach.UseAbility(Abilities.BurrowRoachUp);
            return;
        }

        if (canTunnel) {
            MoveAwayFrom(roach, _terrainTracker.GetClosestWalkable(collidingUnits.GetCenter(), searchRadius: 3));
        }
    }

    private IEnumerable<Unit> GetCollidingUnits(Unit unit, bool checkUnderground) {
        return _unitsTracker.UnitsByTag.Values
            .Where(otherUnit => !Units.Buildings.Contains(otherUnit.UnitType)) // We don't care about the buildings
            .Where(otherUnit => !otherUnit.IsFlying) // Flying units can't collide with ground units
            .Where(otherUnit => otherUnit.IsBurrowed == checkUnderground)
            .Where(otherUnit => otherUnit != unit)
            .Where(otherUnit => otherUnit.DistanceTo(unit) < (otherUnit.Radius + unit.Radius) * 0.90); // Some overlap is possible
    }

    /// <summary>
    /// Try tunneling to safety.
    /// Return true if the unit was given an order, false otherwise.
    /// </summary>
    /// <param name="roach">The roach that needs to tunnel to safety.</param>
    /// <returns>True if the unit was given an order, false otherwise.</returns>
    private bool TunnelToSafety(Unit roach) {
        return TunnelOutOfEnemyRange(roach) || PrepareForResurfacing(roach);
    }

    /// <summary>
    /// Try tunneling out of enemy range.
    /// Return true if the unit was given an order, false otherwise.
    /// </summary>
    /// <param name="roach">The roach that needs to tunnel out of enemy range.</param>
    /// <returns>True if the unit was given an order, false otherwise.</returns>
    private bool TunnelOutOfEnemyRange(Unit roach) {
        var enemiesCanHitUs = Controller.GetUnits(_unitsTracker.EnemyUnits, Units.Military).Any(enemy => enemy.IsInAttackRangeOf(roach));
        if (!enemiesCanHitUs) {
            return false;
        }

        // Run to safety
        var safestRegion = Controller.GetUnits(_unitsTracker.OwnedUnits, Units.TownHalls)
            .Select(townHall => townHall.GetRegion())
            .MinBy(region => _regionsEvaluationsTracker.GetForce(region, Alliance.Enemy));

        safestRegion ??= _regionsTracker.Regions.MinBy(region => _regionsEvaluationsTracker.GetForce(region, Alliance.Enemy));

        roach.Move(safestRegion!.Center);

        return true;
    }

    /// <summary>
    /// Make sure the roach will be able to resurface when the time comes.
    /// If there's a unit above, try to find an empty space.
    /// Return true if the unit was given an order, false otherwise.
    /// </summary>
    /// <param name="roach">The roach that needs to be ready to resurface.</param>
    /// <returns>True if the unit was given an order, false otherwise.</returns>
    private bool PrepareForResurfacing(Unit roach) {
        var collidingUnits = GetCollidingUnits(roach, checkUnderground: false).ToList();
        if (collidingUnits.Count <= 0) {
            return false;
        }

        // We might want to consider enemy units as to not wiggle between colliding units and enemies?
        MoveAwayFrom(roach, _terrainTracker.GetClosestWalkable(collidingUnits.GetCenter(), searchRadius: 3));

        return true;
    }

    /// <summary>
    /// Move the given roach away from a given position.
    /// </summary>
    /// <param name="roach">The roach to move.</param>
    /// <param name="position">The position to move away from.</param>
    private static void MoveAwayFrom(Unit roach, Vector2 position) {
        // TODO Check if you're creating a bottleneck
        roach.Move(roach.Position.ToVector2().TranslateAwayFrom(position, 1f), precision: 0.25f);
    }
}
