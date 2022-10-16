namespace Bot.Managers.ScoutManagement.ScoutSupervision;

public partial class ScoutSupervisor {
    private class ScoutSupervisorReleaser : Releaser<ScoutSupervisor> {
        public ScoutSupervisorReleaser(ScoutSupervisor client) : base(client) {
        }

        public override void Release(Unit unit) {

        }
    }
}
