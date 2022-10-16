namespace Bot.Managers.ScoutManagement.ScoutSupervision;

public partial class ScoutSupervisor {
    private class ScoutSupervisorAssigner : Assigner<ScoutSupervisor> {
        public ScoutSupervisorAssigner(ScoutSupervisor client) : base(client) {
        }

        public override void Assign(Unit unit) {

        }
    }
}
