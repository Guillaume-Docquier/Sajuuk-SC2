using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.GameSense.RegionTracking;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.Managers.WarManagement.States.EarlyGame;

public class EarlyGameRecruitmentPhaseStrategy : WarManagerStrategy {
    private static readonly HashSet<uint> ManageableUnitTypes = Units.ZergMilitary.Except(new HashSet<uint> { Units.Queen, Units.QueenBurrowed }).ToHashSet();

    private readonly HashSet<Region> _startingRegions;
    private bool _rushTagged = false;
    private bool _isRushInProgress = false;


    public EarlyGameRecruitmentPhaseStrategy(WarManager context) : base(context) {
        var main = ExpandAnalyzer.GetExpand(Alliance.Self, ExpandType.Main).Position.GetRegion();
        var natural = ExpandAnalyzer.GetExpand(Alliance.Self, ExpandType.Natural).Position.GetRegion();
        _startingRegions = Pathfinder.FindPath(main, natural).ToHashSet();
    }

    public override void Execute() {
        WarManager.Assign(Controller.GetUnits(UnitsTracker.NewOwnedUnits, ManageableUnitTypes));
        RecruitEcoUnitsIfNecessary();
    }

    public override bool CleanUp() {
        if (_isRushInProgress) {
            return false;
        }

        var draftedUnits = GetDraftedUnits();
        if (draftedUnits.Any()) {
            WarManager.Release(draftedUnits);

            // We give one tick so that release orders, like stop or unburrow go through
            return false;
        }

        return true;
    }

    private void RecruitEcoUnitsIfNecessary() {
        var draftedUnits = GetDraftedUnits();

        // TODO GD To do this we need the eco manager to not send them to a dangerous expand
        //Release(draftedUnits.Where(unit => unit.HitPoints <= 10));

        _isRushInProgress = IsRushInProgress(WarManager.ManagedUnits.Except(draftedUnits).ToList());
        if (!_isRushInProgress) {
            WarManager.Release(draftedUnits);
            return;
        }

        if (!_rushTagged) {
            TaggingService.TagGame(TaggingService.Tag.EarlyAttack);
            _rushTagged = true;
        }

        var townHallSupervisors = Controller
            .GetUnits(UnitsTracker.OwnedUnits, Units.TownHalls)
            .Where(unit => unit.Supervisor != null)
            .Select(supervisedTownHall => supervisedTownHall.Supervisor)
            .ToList();

        var draftableDrones = new List<Unit>();
        foreach (var townHallSupervisor in townHallSupervisors) {
            WarManager.Assign(Controller.GetUnits(townHallSupervisor.SupervisedUnits, Units.Queen));

            draftableDrones
                .AddRange(Controller.GetUnits(townHallSupervisor.SupervisedUnits, Units.Drone)
                // TODO GD Maybe sometimes we should take all of them
                .Skip(2));
        }

        draftableDrones = draftableDrones
            .OrderByDescending(drone => drone.Integrity)
            // TODO GD This could be better, it assumes the threat comes from the natural
            .ThenBy(drone => drone.DistanceTo(ExpandAnalyzer.GetExpand(Alliance.Self, ExpandType.Natural).Position))
            .ToList();

        var enemyForce = GetEnemyForce();
        var draftIndex = 0;
        while (draftIndex < draftableDrones.Count && WarManager.ManagedUnits.GetForce() < enemyForce) {
            WarManager.Assign(draftableDrones[draftIndex]);
            draftIndex++;
        }
    }

    /// <summary>
    /// Returns the global enemy force
    /// </summary>
    /// <returns></returns>
    private static float GetEnemyForce() {
        // TODO GD Change EnemyMemorizedUnits to include all units that we know of
        return UnitsTracker.EnemyMemorizedUnits.Values.Concat(UnitsTracker.EnemyUnits).GetForce();
    }

    /// <summary>
    /// Determines if a rush is in progress by comparing our army to the enemy's
    /// </summary>
    /// <param name="ownArmy"></param>
    /// <returns></returns>
    private bool IsRushInProgress(IEnumerable<Unit> ownArmy) {
        var enemyForce = _startingRegions.Sum(region => RegionTracker.GetForce(region, Alliance.Enemy));
        var ownForce = ownArmy
            .Where(soldier => _startingRegions.Contains(soldier.GetRegion()))
            .GetForce();

        return enemyForce > ownForce;
    }

    /// <summary>
    /// Gets all eco units drafted to help defending
    /// </summary>
    /// <returns>All the eco units drafted to help defending</returns>
    private List<Unit> GetDraftedUnits() {
        return WarManager.ManagedUnits
            .Where(soldier => !ManageableUnitTypes.Contains(soldier.UnitType))
            .ToList();
    }
}
