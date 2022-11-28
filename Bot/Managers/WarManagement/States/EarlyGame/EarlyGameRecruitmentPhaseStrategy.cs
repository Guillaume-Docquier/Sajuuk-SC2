using System.Collections.Generic;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapKnowledge;
using SC2APIProtocol;

namespace Bot.Managers.WarManagement.States.EarlyGame;

public class EarlyGameRecruitmentPhaseStrategy : WarManagerStrategy {
    private bool _rushTagged = false;
    private bool _isRushInProgress = false;

    private static readonly HashSet<uint> ManageableUnitTypes = Units.ZergMilitary.Except(new HashSet<uint> { Units.Queen, Units.QueenBurrowed }).ToHashSet();

    public EarlyGameRecruitmentPhaseStrategy(WarManager context) : base(context) {}

    public override void Execute() {
        WarManager.Assign(Controller.GetUnits(UnitsTracker.NewOwnedUnits, ManageableUnitTypes));
        RecruitEcoUnitsIfNecessary();
    }

    public override bool CanTransition() {
        return !_isRushInProgress;
    }

    private void RecruitEcoUnitsIfNecessary() {
        // TODO GD Hold these in a separate list
        var draftedUnits = WarManager.ManagedUnits
            .Where(soldier => !ManageableUnitTypes.Contains(soldier.UnitType))
            .ToList();

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

            // TODO GD Maybe sometimes we should take all of them
            draftableDrones.AddRange(Controller.GetUnits(townHallSupervisor.SupervisedUnits, Units.Drone).Skip(2));
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
    /// Returns the enemy force
    /// </summary>
    /// <returns></returns>
    private static float GetEnemyForce() {
        // TODO GD Change EnemyMemorizedUnits to include all units that we know of
        return UnitsTracker.EnemyMemorizedUnits.Values.Concat(UnitsTracker.EnemyUnits).GetForce();
    }

    private static bool IsRushInProgress(IReadOnlyCollection<Unit> ownArmy) {
        var main = ExpandAnalyzer.GetExpand(Alliance.Ally, ExpandType.Main).Position.GetRegion();
        var natural = ExpandAnalyzer.GetExpand(Alliance.Ally, ExpandType.Natural).Position.GetRegion();

        var regionsToProtect = Pathfinder.FindPath(main, natural);

        // TODO GD Per region or globally?
        foreach (var regionToProtect in regionsToProtect) {
            var enemyForce = UnitsTracker.EnemyUnits
                .Where(soldier => soldier.GetRegion() == regionToProtect)
                .Sum(UnitEvaluator.EvaluateForce);

            var ownForce = ownArmy
                .Where(soldier => soldier.GetRegion() == regionToProtect)
                .Sum(UnitEvaluator.EvaluateForce);

            if (enemyForce > ownForce) {
                return true;
            }
        }

        return false;
    }
}
