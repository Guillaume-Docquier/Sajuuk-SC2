namespace Sajuuk.Managers.ScoutManagement;

public partial class ScoutManager {
    private class ScoutManagerReleaser : Releaser<ScoutManager> {
        public ScoutManagerReleaser(ScoutManager client) : base(client) {}

        public override void Release(Unit unit) {
        }
    }
}
