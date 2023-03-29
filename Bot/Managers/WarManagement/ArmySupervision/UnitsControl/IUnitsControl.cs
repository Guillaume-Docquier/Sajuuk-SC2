using System.Collections.Generic;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl;

public interface IUnitsControl {
    bool IsExecuting();

    // TODO GD Profile performance of mutating the hashset vs returning a new one
    /// <summary>
    /// Executes the units control.
    /// Units that are controlled will be removed from the army.
    /// </summary>
    /// <param name="army"></param>
    IReadOnlySet<Unit> Execute(IReadOnlySet<Unit> army);

    // TODO GD We don't actually provide the army when resetting. We should either provide it or remove the param
    void Reset(IReadOnlyCollection<Unit> army);
}
