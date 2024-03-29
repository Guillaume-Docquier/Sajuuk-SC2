﻿using System;
using System.Linq;
using Sajuuk.ExtensionMethods;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.GameData;
using Sajuuk.GameSense;
using Sajuuk.Utils;

namespace Sajuuk.UnitModules;

public class TumorCreepSpreadModule: UnitModule {
    public const string ModuleTag = "TumorCreepSpreadModule";

    private readonly IVisibilityTracker _visibilityTracker;
    private readonly ITerrainTracker _terrainTracker;
    private readonly IBuildingTracker _buildingTracker;
    private readonly ICreepTracker _creepTracker;
    private readonly IRegionsTracker _regionsTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;
    private readonly IFrameClock _frameClock;

    private const int CreepSpreadCooldown = 12;
    private const double EnemyBaseConvergenceFactor = 0.33;

    private readonly Unit _creepTumor;
    private ulong _spreadAt = default;

    public TumorCreepSpreadModule(
        IVisibilityTracker visibilityTracker,
        ITerrainTracker terrainTracker,
        IBuildingTracker buildingTracker,
        ICreepTracker creepTracker,
        IRegionsTracker regionsTracker,
        IGraphicalDebugger graphicalDebugger,
        IFrameClock frameClock,
        Unit creepTumor
    ) : base(ModuleTag) {
        _visibilityTracker = visibilityTracker;
        _terrainTracker = terrainTracker;
        _buildingTracker = buildingTracker;
        _creepTracker = creepTracker;
        _regionsTracker = regionsTracker;
        _graphicalDebugger = graphicalDebugger;
        _frameClock = frameClock;

        _creepTumor = creepTumor;
    }

    // TODO GD Move this code in CreepManager
    protected override void DoExecute() {
        if (_spreadAt == default) {
            if (_creepTumor.UnitType == Units.CreepTumorBurrowed) {
                _spreadAt = _frameClock.CurrentFrame + TimeUtils.SecsToFrames(CreepSpreadCooldown);
            }
        }
        else if (_spreadAt <= _frameClock.CurrentFrame) {
            var creepFrontier = _creepTracker.GetCreepFrontier();
            if (creepFrontier.Count == 0) {
                return;
            }

            var creepTarget = creepFrontier.MinBy(creepNode => creepNode.DistanceTo(_creepTumor) + creepNode.DistanceTo(_terrainTracker.EnemyStartingLocation) * EnemyBaseConvergenceFactor);

            // Make sure not to go too far out
            var spreadRange = (int)Math.Floor(_creepTumor.UnitTypeData.SightRange - 0.5);

            var bestPlaceLocation = _terrainTracker.BuildSearchRadius(_creepTumor.Position.ToVector2(), spreadRange)
                .Where(_visibilityTracker.IsVisible)
                .Where(_creepTracker.HasCreep)
                .Where(position => !_regionsTracker.IsBlockingExpand(position))
                .OrderBy(position => position.DistanceTo(creepTarget))
                .FirstOrDefault(position => _buildingTracker.CanPlace(Units.CreepTumor, position));

            if (bestPlaceLocation == default) {
                return;
            }

            _creepTumor.UseAbility(Abilities.SpawnCreepTumor, position: bestPlaceLocation.ToPoint2D());
            _graphicalDebugger.AddSphere(_terrainTracker.WithWorldHeight(bestPlaceLocation), 1, Colors.Yellow);

            Uninstall<TumorCreepSpreadModule>(_creepTumor);
        }
    }
}
