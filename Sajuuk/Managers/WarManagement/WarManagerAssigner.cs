using Sajuuk.UnitModules;

namespace Sajuuk.Managers.WarManagement;

public class WarManagerAssigner<T>: Assigner<T> {
    private readonly IUnitModuleInstaller _unitModuleInstaller;

    public WarManagerAssigner(
        IUnitModuleInstaller unitModuleInstaller,
        T client
    ) : base(client) {
        _unitModuleInstaller = unitModuleInstaller;
    }

    public override void Assign(Unit unit) {
        Logger.Debug("({0}) Assigned {1}", Client, unit);
        _unitModuleInstaller.InstallChangelingTargetingModule(unit);
        _unitModuleInstaller.InstallAttackPriorityModule(unit);
    }
}
