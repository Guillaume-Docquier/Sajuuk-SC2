namespace Bot.Managers.ScoutManagement;

public partial class ScoutManager {
    private class ScoutManagerAssigner : Assigner<ScoutManager> {
        public ScoutManagerAssigner(ScoutManager client) : base(client) {}

        public override void Assign(Unit unit) {
        }
    }
}
