using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapKnowledge;

namespace Bot.UnitModules;

public class QueenMicroModule: UnitModule, IWatchUnitsDie {
    public const string Tag = "QueenMicroModule";

    private Unit _queen;
    private Unit _assignedTownHall;

    public static void Install(Unit queen, Unit assignedTownHall = null) {
        if (PreInstallCheck(Tag, queen)) {
            queen.Modules.Add(Tag, new QueenMicroModule(queen, assignedTownHall));
        }
    }

    private QueenMicroModule(Unit queen, Unit assignedTownHall) {
        _queen = queen;
        _queen.AddDeathWatcher(this);

        AssignTownHall(assignedTownHall);
    }

    public void AssignTownHall(Unit townHall) {
        _assignedTownHall = townHall;
        _assignedTownHall?.AddDeathWatcher(this);
    }

    protected override void DoExecute() {
        if (_queen == null) {
            return;
        }

        if (_queen.Orders.Any()) {
            return;
        }

        if (_assignedTownHall != null && _queen.HasEnoughEnergy(Abilities.InjectLarvae)) {
            _queen.UseAbility(Abilities.InjectLarvae, targetUnitTag: _assignedTownHall.Tag);
        }
        else if (_queen.HasEnoughEnergy(Abilities.SpawnCreepTumor)) {
            var tumorPosition = CreepTracker.GetCreepFrontier()
                .Where(ExpandAnalyzer.IsNotBlockingExpand)
                .OrderBy(creepNode => _queen.HorizontalDistanceTo(creepNode))
                .FirstOrDefault(creepNode => BuildingTracker.CanPlace(Units.CreepTumor, creepNode) && Pathfinder.FindPath(_queen.Position, creepNode) != null);

            if (tumorPosition != default) {
                _queen.UseAbility(Abilities.SpawnCreepTumor, position: tumorPosition.ToPoint2D());
            }
        }
    }

    public void ReportUnitDeath(Unit deadUnit) {
        if (deadUnit == _assignedTownHall) {
            _assignedTownHall = null;
        }
        else if (deadUnit == _queen) {
            _queen = null;
        }
    }
}
