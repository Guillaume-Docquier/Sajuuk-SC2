using System.Collections.Generic;
using Bot.Algorithms;
using Bot.GameSense;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl.SneakAttackUnitsControl;

public partial class SneakAttack {
    public class TerminalState: SneakAttackState {
        private readonly IUnitsTracker _unitsTracker;
        private readonly ITerrainTracker _terrainTracker;
        private readonly IFrameClock _frameClock;
        private readonly IDetectionTracker _detectionTracker;
        private readonly IUnitEvaluator _unitEvaluator;
        private readonly IClustering _clustering;

        public TerminalState(
            IUnitsTracker unitsTracker,
            ITerrainTracker terrainTracker,
            IFrameClock frameClock,
            IDetectionTracker detectionTracker,
            IUnitEvaluator unitEvaluator,
            IClustering clustering
        ) {
            _unitsTracker = unitsTracker;
            _terrainTracker = terrainTracker;
            _frameClock = frameClock;
            _detectionTracker = detectionTracker;
            _unitEvaluator = unitEvaluator;
            _clustering = clustering;
        }

        public override bool IsViable(IReadOnlyCollection<Unit> army) {
            return false;
        }

        protected override void Execute() {
            Logger.Error("TerminalState should never be executed");
            NextState = new InactiveState(_unitsTracker, _terrainTracker, _frameClock, _detectionTracker, _unitEvaluator, _clustering);
        }
    }
}
