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

            if (buildRequestFulfillment.Status.HasFlag(BuildRequestFulfillmentStatus.Terminated)) {
                _inProgressFulfillments.Remove(buildRequestFulfillment);
                _completedFulfillments.Add(buildRequestFulfillment);
            }
        }
    }

    public IEnumerable<IBuildRequestFulfillment> FulfillmentsInProgress => _inProgressFulfillments;

    public void TrackFulfillment(IBuildRequestFulfillment buildRequestFulfillment) {
        if (buildRequestFulfillment.Status.HasFlag(BuildRequestFulfillmentStatus.Terminated)) {
            Logger.Warning($"You are registering a completed fulfillment to the fulfillment tracker. It should have been in progress. {buildRequestFulfillment}");
            _completedFulfillments.Add(buildRequestFulfillment);

            return;
        }

        _inProgressFulfillments.Add(buildRequestFulfillment);
    }
}
