using System.Linq;
using Sajuuk.ExtensionMethods;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.MapAnalysis;

namespace Sajuuk.UnitModules;

public class QueenMicroModule: UnitModule, IWatchUnitsDie {
    public const string ModuleTag = "QueenMicroModule";

    private readonly IBuildingTracker _buildingTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly ICreepTracker _creepTracker;
    private readonly IPathfinder _pathfinder;

    private Unit _queen;
    private Unit _assignedTownHall;

    public QueenMicroModule(
        IBuildingTracker buildingTracker,
        IRegionsTracker regionsTracker,
        ICreepTracker creepTracker,
        IPathfinder pathfinder,
        Unit queen,
        Unit assignedTownHall
    ) : base(ModuleTag) {
        _buildingTracker = buildingTracker;
        _regionsTracker = regionsTracker;
        _creepTracker = creepTracker;
        _pathfinder = pathfinder;

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
            var tumorPosition = _creepTracker.GetCreepFrontier()
                .Where(creepNode => !_regionsTracker.IsBlockingExpand(creepNode))
                .OrderBy(creepNode => _queen.DistanceTo(creepNode)) // TODO GD Try to favor between bases and towards the enemy
                .FirstOrDefault(creepNode => _buildingTracker.CanPlace(Units.CreepTumor, creepNode) && _pathfinder.FindPath(_queen.Position.ToVector2(), creepNode) != null);

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
