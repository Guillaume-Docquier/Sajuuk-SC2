using System.Collections.Generic;
using Bot.Builds;

namespace Bot.Managers;

public interface IManager: IWatchUnitsDie {
    IEnumerable<BuildFulfillment> BuildFulfillments { get; }

    void OnFrame();

    void Release(Unit unit);

    void Retire();
}
