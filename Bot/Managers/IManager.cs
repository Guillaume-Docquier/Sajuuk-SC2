using System.Collections.Generic;

namespace Bot.Managers;

public interface IManager: IWatchUnitsDie {
    IEnumerable<BuildOrders.BuildStep> BuildStepRequests { get; }

    void OnFrame();
}
