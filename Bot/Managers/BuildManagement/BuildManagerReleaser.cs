namespace Bot.Managers.BuildManagement;

public partial class BuildManager {
    public class BuildManagerReleaser : Releaser<BuildManager> {
        public BuildManagerReleaser(BuildManager client) : base(client) {
        }

        public override void Release(Unit unit) {
            throw new System.NotImplementedException();
        }
    }
}
