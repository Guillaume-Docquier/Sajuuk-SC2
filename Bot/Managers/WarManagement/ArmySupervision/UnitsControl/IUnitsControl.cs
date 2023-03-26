using System.Collections.Generic;

namespace Bot.Managers.WarManagement.ArmySupervision.UnitsControl;

public interface IUnitsControl {
    bool IsViable(IReadOnlyCollection<Unit> army);

    bool IsExecuting();

    void Execute(IReadOnlyCollection<Unit> army);

    void Reset(IReadOnlyCollection<Unit> army);
}
