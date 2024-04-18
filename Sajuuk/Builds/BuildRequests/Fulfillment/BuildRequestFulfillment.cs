namespace Sajuuk.Builds.BuildRequests.Fulfillment;

public abstract class BuildRequestFulfillment : IBuildRequestFulfillment {
    private BuildRequestFulfillmentStatus _status = BuildRequestFulfillmentStatus.None;

    public BuildRequestFulfillmentStatus Status {
        get => _status;
        protected set {
            if (_status != value) {
                _status = value;

                var statusChangeMessage = $"{this} is {_status.ToString()}";
                if (_status.HasFlag(BuildRequestFulfillmentStatus.Failure)) {
                    Logger.Failure(statusChangeMessage);
                }
                else if (_status.HasFlag(BuildRequestFulfillmentStatus.Successful)) {
                    Logger.Success(statusChangeMessage);
                }
                else {
                    Logger.Info(statusChangeMessage);
                }
            }
        }
    }

    public void Abort() {
        Status = BuildRequestFulfillmentStatus.Aborted;
    }

    public void Cancel() {
        // TODO GD How to cancel?
        throw new System.NotImplementedException();

        Status = BuildRequestFulfillmentStatus.Canceled;
    }


    public abstract ulong ExpectedCompletionFrame { get; }
    public abstract void UpdateStatus();
    public abstract bool CanSatisfy(IBuildRequest buildRequest);
}
