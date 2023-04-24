using Bot.GameSense;
using Bot.UnitModules;

namespace Bot.Managers.WarManagement;

public class WarManagerAssigner<T>: Assigner<T> {
    private readonly IUnitsTracker _unitsTracker;

    public WarManagerAssigner(T client, IUnitsTracker unitsTracker) : base(client) {
        _unitsTracker = unitsTracker;
    }

    public override void Assign(Unit unit) {
        Logger.Debug("({0}) Assigned {1}", Client, unit);
        ChangelingTargetingModule.Install(unit, _unitsTracker);
        AttackPriorityModule.Install(unit, _unitsTracker);
    }
}
