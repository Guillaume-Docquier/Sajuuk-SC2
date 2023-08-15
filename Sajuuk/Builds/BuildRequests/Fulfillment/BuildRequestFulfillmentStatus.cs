namespace Sajuuk.Builds.BuildRequests.Fulfillment;

public enum BuildRequestFulfillmentStatus {
    /// <summary>
    /// The fulfillment needs preparation before starting.
    /// This can be reaching a construction location, for example, or being a queued order.
    /// </summary>
    Preparing,

    /// <summary>
    /// The fulfillment has started, but can still be canceled.
    /// </summary>
    InProgress,

    /// <summary>
    /// The fulfilment was completed.
    /// </summary>
    Completed,

    /// <summary>
    /// The fulfillment was canceled by us and will not complete.
    /// </summary>
    Canceled,

    /// <summary>
    /// The fulfillment was prevented by the opponent and will not complete.
    /// </summary>
    Prevented
}
