using Bot.UnitModules;

namespace Bot.Managers;

public partial class WarManager {
    private class WarManagerAssigner: IAssigner {
        private readonly WarManager _manager;

        public WarManagerAssigner(WarManager manager) {
            _manager = manager;
        }

        public void Assign(Unit unit) {
            Logger.Debug("({0}) Assigned {1}", _manager, unit);

            _manager._soldiers.Add(unit);
            ChangelingTargetingModule.Install(unit);
        }
    }
}
