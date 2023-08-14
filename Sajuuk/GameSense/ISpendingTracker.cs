using System.Collections.Generic;
using Sajuuk.Builds.BuildRequests;

namespace Sajuuk.GameSense;

public interface ISpendingTracker {
    public float ExpectedFutureMineralsSpending { get; }
    public float ExpectedFutureVespeneSpending { get; }

    /// <summary>
    /// Update the expected future spending.
    /// We look at the future build requests and we compile how much minerals and gas we want to spend.
    ///
    /// We limit the sum to the minerals and gas income of the next minute.
    /// This effectively prioritizes expenses that will happen soon.
    /// </summary>
    /// <param name="futureBuildRequests"></param>
    public void UpdateExpectedFutureSpending(List<IFulfillableBuildRequest> futureBuildRequests);
}
