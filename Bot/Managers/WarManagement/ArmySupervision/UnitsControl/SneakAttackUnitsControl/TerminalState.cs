using System.Collections.Generic;
using Bot.GameSense;
using Bot.MapKnowledge;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl.SneakAttackUnitsControl;

public partial class SneakAttack {
    public class TerminalState: SneakAttackState {
        private readonly IUnitsTracker _unitsTracker;
        private readonly IMapAnalyzer _mapAnalyzer;

        public TerminalState(IUnitsTracker unitsTracker, IMapAnalyzer mapAnalyzer) {
            _unitsTracker = unitsTracker;
            _mapAnalyzer = mapAnalyzer;
        }

        public override bool IsViable(IReadOnlyCollection<Unit> army) {
            return false;
        }

        protected override void Execute() {
            Logger.Error("TerminalState should never be executed");
            NextState = new InactiveState(_unitsTracker, _mapAnalyzer);
        }
    }
}
