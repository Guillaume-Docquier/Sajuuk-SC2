using System;
using System.Linq;
using Bot.ExtensionMethods;
using Bot.GameData;
using Bot.GameSense;
using Bot.MapKnowledge;
using Bot.Wrapper;

namespace Bot.UnitModules;

public class TumorCreepSpreadModule: IUnitModule {
    public const string Tag = "tumor-creep-spread-module";

    private const int CreepSpreadCooldown = 12;
    private const double EnemyBaseConvergenceFactor = 0.5;

    private readonly Unit _creepTumor;
    private ulong _spreadAt = default;

    public static void Install(Unit unit) {
        if (UnitModule.PreInstallCheck(Tag, unit)) {
            unit.Modules.Add(Tag, new TumorCreepSpreadModule(unit));
        }
    }

    private TumorCreepSpreadModule(Unit creepTumor) {
        _creepTumor = creepTumor;
    }

    public void Execute() {
        if (_spreadAt == default) {
            if (_creepTumor.UnitType == Units.CreepTumorBurrowed) {
                _spreadAt = Controller.Frame + Controller.SecsToFrames(CreepSpreadCooldown);
            }
        }
        else if (_spreadAt <= Controller.Frame) {
            var creepTarget = CreepTracker.GetCreepFrontier()
                .MinBy(creepNode => creepNode.HorizontalDistanceTo(_creepTumor) + creepNode.HorizontalDistanceTo(MapAnalyzer.EnemyStartingLocation) * EnemyBaseConvergenceFactor);

            // Make sure not to go too far out
            var spreadRange = (int)Math.Floor(_creepTumor.UnitTypeData.SightRange - 0.5);

            var bestPlaceLocation = MapAnalyzer.BuildSearchRadius(_creepTumor.Position, spreadRange)
                .Where(VisibilityTracker.IsVisible)
                .Where(CreepTracker.HasCreep)
                .Where(ExpandAnalyzer.IsNotBlockingExpand)
                .OrderBy(position => position.HorizontalDistanceTo(creepTarget))
                .FirstOrDefault(position => BuildingTracker.CanPlace(Units.CreepTumor, position));

            if (bestPlaceLocation != default) {
                _creepTumor.UseAbility(Abilities.SpawnCreepTumor, position: bestPlaceLocation.ToPoint2D());
                GraphicalDebugger.AddSphere(bestPlaceLocation, 1, Colors.Yellow);

                UnitModule.Uninstall<TumorCreepSpreadModule>(_creepTumor);
            }
        }
    }
}
