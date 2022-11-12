using Bot.UnitModules;

namespace Bot.Managers.WarManagement;

public partial class WarManager {
    private class WarManagerAssigner: Assigner<WarManager> {
        public WarManagerAssigner(WarManager client) : base(client) {}

        public override void Assign(Unit unit) {
            Logger.Debug("({0}) Assigned {1}", Client, unit);

            Client._soldiers.Add(unit);
            ChangelingTargetingModule.Install(unit);
        }
    }
}
