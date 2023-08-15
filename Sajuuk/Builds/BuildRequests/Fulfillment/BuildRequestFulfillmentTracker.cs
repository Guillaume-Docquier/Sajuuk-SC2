using System.Collections.Generic;
using System.Linq;
using SC2APIProtocol;

namespace Sajuuk.Builds.BuildRequests.Fulfillment;

// TODO GD Should fulfillments always be registered? If so, should this be a fulfillment factory?
// TODO GD Should this be a tracker? Since fulfillments should be updated after all other trackers, should this be handled by Sajuuk instead?
public class BuildRequestFulfillmentTracker : INeedUpdating, IBuildRequestFulfillmentTracker {
    private readonly HashSet<IBuildRequestFulfillment> _inProgressFulfillments = new HashSet<IBuildRequestFulfillment>();
    private readonly List<IBuildRequestFulfillment> _completedFulfillments = new List<IBuildRequestFulfillment>();

    public void Update(ResponseObservation observation, ResponseGameInfo gameInfo) {
        foreach (var buildRequestFulfillment in _inProgressFulfillments.ToList()) {
            // TODO GD This creates an implicit dependency from this tracker to other trackers needed by the fulfillment
            buildRequestFulfillment.UpdateStatus();

            if (IsCompleted(buildRequestFulfillment.Status)) {
                _inProgressFulfillments.Remove(buildRequestFulfillment);
                _completedFulfillments.Add(buildRequestFulfillment);
            }
        }
    }

    public void RegisterBuildFulfillment(IBuildRequestFulfillment buildRequestFulfillment) {
        if (IsCompleted(buildRequestFulfillment.Status)) {
            Logger.Warning($"You are registering a completed fulfillment to the fulfillment tracker. It should have been in progress. {buildRequestFulfillment}");
            _completedFulfillments.Add(buildRequestFulfillment);

            return;
        }

        _inProgressFulfillments.Add(buildRequestFulfillment);
    }

    private static bool IsCompleted(BuildRequestFulfillmentStatus buildRequestFulfillmentStatus) {
        return buildRequestFulfillmentStatus
            is BuildRequestFulfillmentStatus.Completed
            or BuildRequestFulfillmentStatus.Canceled
            or BuildRequestFulfillmentStatus.Prevented;
    }
}
