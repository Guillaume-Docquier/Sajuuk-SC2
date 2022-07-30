using System.Collections.Generic;

namespace Bot.Managers.ArmyManagement.Tactics;

public interface ITactic {
    bool IsViable(IReadOnlyCollection<Unit> army);

    void Execute(IReadOnlyCollection<Unit> army);

    void Reset(IReadOnlyCollection<Unit> army);
}
