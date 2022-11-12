namespace Bot.Managers.BuildManagement;

public partial class BuildManager {
    public class BuildManagerAssigner : Assigner<BuildManager> {
        public BuildManagerAssigner(BuildManager client) : base(client) {
        }

        public override void Assign(Unit unit) {
            throw new System.NotImplementedException();
        }
    }
}
