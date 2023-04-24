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
    public const string Tag = "TumorCreepSpreadModule";

    private const int CreepSpreadCooldown = 12;
    private const double EnemyBaseConvergenceFactor = 0.33;

    private readonly IVisibilityTracker _visibilityTracker;

    private readonly Unit _creepTumor;
    private ulong _spreadAt = default;

    private TumorCreepSpreadModule(IVisibilityTracker visibilityTracker, Unit creepTumor) {
        _visibilityTracker = visibilityTracker;
        _creepTumor = creepTumor;
    }

    public static void Install(IVisibilityTracker visibilityTracker, Unit unit) {
        if (PreInstallCheck(Tag, unit)) {
            unit.Modules.Add(Tag, new TumorCreepSpreadModule(visibilityTracker, unit));
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

            var creepTarget = creepFrontier.MinBy(creepNode => creepNode.DistanceTo(_creepTumor) + creepNode.DistanceTo(MapAnalyzer.EnemyStartingLocation) * EnemyBaseConvergenceFactor);

            // Make sure not to go too far out
            var spreadRange = (int)Math.Floor(_creepTumor.UnitTypeData.SightRange - 0.5);

            var bestPlaceLocation = MapAnalyzer.BuildSearchRadius(_creepTumor.Position.ToVector2(), spreadRange)
                .Where(_visibilityTracker.IsVisible)
                .Where(CreepTracker.HasCreep)
                .Where(ExpandAnalyzer.IsNotBlockingExpand)
                .OrderBy(position => position.DistanceTo(creepTarget))
                .FirstOrDefault(position => BuildingTracker.Instance.CanPlace(Units.CreepTumor, position));

            if (bestPlaceLocation == default) {
                return;
            }

            _creepTumor.UseAbility(Abilities.SpawnCreepTumor, position: bestPlaceLocation.ToPoint2D());
            Program.GraphicalDebugger.AddSphere(bestPlaceLocation.ToVector3(), 1, Colors.Yellow);

            Uninstall<TumorCreepSpreadModule>(_creepTumor);
        }
    }
}
