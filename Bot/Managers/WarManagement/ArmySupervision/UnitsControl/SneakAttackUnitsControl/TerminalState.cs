using System.Collections.Generic;
using Bot.GameSense;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl.SneakAttackUnitsControl;

public partial class SneakAttack {
    public class TerminalState: SneakAttackState {
        private readonly IUnitsTracker _unitsTracker;
        private readonly ITerrainTracker _terrainTracker;

        public TerminalState(IUnitsTracker unitsTracker, ITerrainTracker terrainTracker) {
            _unitsTracker = unitsTracker;
            _terrainTracker = terrainTracker;
        }

        public override bool IsViable(IReadOnlyCollection<Unit> army) {
            return false;
        }

        protected override void Execute() {
            Logger.Error("TerminalState should never be executed");
            NextState = new InactiveState(_unitsTracker, _terrainTracker);
        }
    }
}
