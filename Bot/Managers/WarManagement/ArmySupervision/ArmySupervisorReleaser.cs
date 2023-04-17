using Bot.UnitModules;

namespace Bot.Managers.WarManagement.ArmySupervision;

public partial class ArmySupervisor {
    private class ArmySupervisorReleaser: IReleaser {
        private readonly ArmySupervisor _supervisor;

        public ArmySupervisorReleaser(ArmySupervisor supervisor) {
            _supervisor = supervisor;
        }

        public void Release(Unit unit) {
            _supervisor.Army.Remove(unit);
            Logger.Debug("({0}) Released {1}", _supervisor, unit);
        }
    }
}
