using System.Collections.Generic;
using Bot.Builds;

namespace Bot.Debugging;

// TODO GD It probably shouldn't live in debugging, it's very tied to how Sajuuk works
public interface IBotDebugger {
    public void Debug(List<BuildFulfillment> managerBuildRequests, (BuildFulfillment, BuildBlockCondition) buildBlockStatus);
}
