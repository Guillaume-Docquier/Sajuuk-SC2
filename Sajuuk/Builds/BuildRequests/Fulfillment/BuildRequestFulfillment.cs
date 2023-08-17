namespace Sajuuk.Builds.BuildRequests.Fulfillment;

public abstract class BuildRequestFulfillment : IBuildRequestFulfillment {
    private BuildRequestFulfillmentStatus _status = BuildRequestFulfillmentStatus.Preparing;
    public BuildRequestFulfillmentStatus Status {
        get => _status;
        protected set {
            if (_status != value) {
                // TODO GD Include more info about the fulfillment
                Logger.Info($"Fulfillment {_status.ToString()}");
                _status = value;
            }
        }
    }

    public void Abort() {
        Status = BuildRequestFulfillmentStatus.Aborted;
        Logger.Info($"Fulfillment {Status.ToString()}");
    }

    public void Cancel() {
        // TODO GD How to cancel?
        throw new System.NotImplementedException();

        Status = BuildRequestFulfillmentStatus.Canceled;
        Logger.Info($"Fulfillment {Status.ToString()}");
    }

    public abstract void UpdateStatus();
    public abstract bool CanSatisfy(IBuildRequest buildRequest);
}
