using Bot.UnitModules;

namespace Bot.Managers.WarManagement;

public class WarManagerAssigner<T>: Assigner<T> {
    public WarManagerAssigner(T client) : base(client) {}

    public override void Assign(Unit unit) {
        Logger.Debug("({0}) Assigned {1}", Client, unit);
        ChangelingTargetingModule.Install(unit);
    }
}
