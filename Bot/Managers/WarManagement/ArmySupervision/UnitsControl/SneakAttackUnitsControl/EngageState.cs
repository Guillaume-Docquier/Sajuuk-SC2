using System.Collections.Generic;
using System.Linq;
using Bot.Algorithms;
using Bot.GameData;
using Bot.GameSense;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl.SneakAttackUnitsControl;

public partial class SneakAttack {
    public class EngageState : SneakAttackState {
        private readonly IUnitsTracker _unitsTracker;
        private readonly ITerrainTracker _terrainTracker;
        private readonly IFrameClock _frameClock;
        private readonly IDetectionTracker _detectionTracker;
        private readonly IUnitEvaluator _unitEvaluator;
        private readonly IClustering _clustering;

        public EngageState(
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
            return true;
        }

        protected override void Execute() {
            UnburrowUnderlings(Context._army);

            NextState = new TerminalState(_unitsTracker, _terrainTracker, _frameClock, _detectionTracker, _unitEvaluator, _clustering);
        }

        private static void UnburrowUnderlings(IEnumerable<Unit> army) {
            foreach (var soldier in army.Where(soldier => soldier.IsBurrowed)) {
                soldier.UseAbility(Abilities.BurrowRoachUp);
            }
        }
    }
}
