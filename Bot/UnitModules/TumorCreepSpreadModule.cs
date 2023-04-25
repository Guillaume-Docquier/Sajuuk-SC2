using System;
using System.Linq;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapKnowledge;
using Bot.Utils;

namespace Bot.UnitModules;

public class TumorCreepSpreadModule: UnitModule {
    private readonly IVisibilityTracker _visibilityTracker;
    private readonly IMapAnalyzer _mapAnalyzer;

    public const string Tag = "TumorCreepSpreadModule";

    private const int CreepSpreadCooldown = 12;
    private const double EnemyBaseConvergenceFactor = 0.33;

    private readonly Unit _creepTumor;
    private ulong _spreadAt = default;

    private TumorCreepSpreadModule(Unit creepTumor, IVisibilityTracker visibilityTracker, IMapAnalyzer mapAnalyzer) {
        _creepTumor = creepTumor;

        _visibilityTracker = visibilityTracker;
        _mapAnalyzer = mapAnalyzer;
    }

    public static void Install(Unit unit, IVisibilityTracker visibilityTracker, IMapAnalyzer mapAnalyzer) {
        if (PreInstallCheck(Tag, unit)) {
            unit.Modules.Add(Tag, new TumorCreepSpreadModule(unit, visibilityTracker, mapAnalyzer));
        }
    }

    // TODO GD Move this code in CreepManager
    protected override void DoExecute() {
        if (_spreadAt == default) {
            if (_creepTumor.UnitType == Units.CreepTumorBurrowed) {
                _spreadAt = Controller.Frame + TimeUtils.SecsToFrames(CreepSpreadCooldown);
            }
        }
        else if (_spreadAt <= Controller.Frame) {
            var creepFrontier = CreepTracker.Instance.GetCreepFrontier();
            if (creepFrontier.Count == 0) {
                return;
            }

            var creepTarget = creepFrontier.MinBy(creepNode => creepNode.DistanceTo(_creepTumor) + creepNode.DistanceTo(_mapAnalyzer.EnemyStartingLocation) * EnemyBaseConvergenceFactor);

            // Make sure not to go too far out
            var spreadRange = (int)Math.Floor(_creepTumor.UnitTypeData.SightRange - 0.5);

            var bestPlaceLocation = _mapAnalyzer.BuildSearchRadius(_creepTumor.Position.ToVector2(), spreadRange)
                .Where(_visibilityTracker.IsVisible)
                .Where(CreepTracker.Instance.HasCreep)
                .Where(ExpandAnalyzer.IsNotBlockingExpand)
                .OrderBy(position => position.DistanceTo(creepTarget))
                .FirstOrDefault(position => BuildingTracker.Instance.CanPlace(Units.CreepTumor, position));

            if (bestPlaceLocation == default) {
                return;
            }

            _creepTumor.UseAbility(Abilities.SpawnCreepTumor, position: bestPlaceLocation.ToPoint2D());
            Program.GraphicalDebugger.AddSphere(_mapAnalyzer.WithWorldHeight(bestPlaceLocation), 1, Colors.Yellow);

            Uninstall<TumorCreepSpreadModule>(_creepTumor);
        }
    }
}
