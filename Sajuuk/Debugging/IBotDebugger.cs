using System.Collections.Generic;
using Sajuuk.Builds;

namespace Sajuuk.Debugging;

// TODO GD It probably shouldn't live in debugging, it's very tied to how Sajuuk works
public interface IBotDebugger {
    public void Debug(List<IFulfillableBuildRequest> managerBuildRequests, (IFulfillableBuildRequest, BuildBlockCondition) buildBlockStatus);
}
