namespace Bot.Managers.BuildManagement;

public partial class BuildManager {
    public class BuildManagerDispatcher : Dispatcher<BuildManager> {
        public BuildManagerDispatcher(BuildManager client) : base(client) {
        }

        public override void Dispatch(Unit unit) {
            throw new System.NotImplementedException();
        }
    }
}
