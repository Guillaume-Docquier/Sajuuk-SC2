using Bot.UnitModules;

namespace Bot.Managers;

public partial class WarManager {
    private class WarManagerAssigner: IAssigner {
        private readonly WarManager _manager;

        public WarManagerAssigner(WarManager manager) {
            _manager = manager;
        }

        public void Assign(Unit unit) {
            _manager._soldiers.Add(unit);
            ChangelingTargetingModule.Install(unit);
        }
    }
}
