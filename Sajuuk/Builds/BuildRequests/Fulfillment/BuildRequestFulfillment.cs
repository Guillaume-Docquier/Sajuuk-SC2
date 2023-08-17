namespace Sajuuk.Builds.BuildRequests.Fulfillment;

public abstract class BuildRequestFulfillment : IBuildRequestFulfillment {
    private BuildRequestFulfillmentStatus _status = BuildRequestFulfillmentStatus.None;

    public BuildRequestFulfillmentStatus Status {
        get => _status;
        protected set {
            if (_status != value) {
                _status = value;
                Logger.Info($"{this} is {_status.ToString()}");
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


    public abstract uint ExpectedCompletionFrame { get; }
    public abstract void UpdateStatus();
    public abstract bool CanSatisfy(IBuildRequest buildRequest);
}
