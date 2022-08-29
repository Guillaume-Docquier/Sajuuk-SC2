using Bot.UnitModules;

namespace Bot.Managers.ArmySupervision;

public partial class ArmySupervisor {
    public class ArmySupervisorReleaser: IReleaser {
        private readonly ArmySupervisor _supervisor;

        public ArmySupervisorReleaser(ArmySupervisor supervisor) {
            _supervisor = supervisor;
        }

        public void Release(Unit unit) {
            _supervisor.Army.Remove(unit);

            UnitModule.Uninstall<BurrowMicroModule>(unit);
            UnitModule.Uninstall<AttackPriorityModule>(unit);

            Logger.Debug("({0}) Released {1}", _supervisor, unit);
        }
    }
}
