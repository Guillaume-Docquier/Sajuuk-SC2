using System;

namespace Sajuuk.Builds.BuildRequests.Fulfillment;

[Flags]
public enum BuildRequestFulfillmentStatus {
    //////////////////////////
    //                      //
    //   Individual flags   //
    //                      //
    //////////////////////////

    /// <summary>
    /// A flag to indicate that the fulfillment is in progress and will eventually terminate.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// A flag to indicate that the fulfillment has yet to start executing.
    /// </summary>
    NotStarted = 2,

    /// <summary>
    /// A flag to indicate that the fulfillment has started executing.
    /// </summary>
    Started = 4,

    /// <summary>
    /// A flag to indicate that the fulfillment has terminated.
    /// </summary>
    Terminated = 8,

    /// <summary>
    /// A flag to indicate that the fulfillment was successful.
    /// </summary>
    Successful = 16,

    /// <summary>
    /// A flag to indicate that the fulfillment failed.
    /// </summary>
    Failure = 32,

    /// <summary>
    /// A flag to indicate that the fulfillment status was caused by us.
    /// </summary>
    ByUs = 64,

    /// <summary>
    /// A flag to indicate that the fulfillment status was caused by some game rule.
    /// </summary>
    ByGameRules = 128,

    /// <summary>
    /// A flag to indicate that the fulfillment status was caused by an opponent.
    /// </summary>
    ByOpponent = 256,

    ////////////////////////
    //                    //
    //   Valid statuses   //
    //                    //
    ////////////////////////

    /// <summary>
    /// A status to indicate that the fulfillment needs preparation before starting.
    /// This can be reaching a construction location, for example, or being a queued order.
    /// </summary>
    Preparing = InProgress + NotStarted,

    /// <summary>
    /// A status to indicate that the fulfillment has started, but can still be canceled or prevented.
    /// </summary>
    Executing = InProgress + Started,

    /// <summary>
    /// A status to indicate that the fulfilment was completed successfully.
    /// </summary>
    Completed = Terminated + Successful + ByUs,

    /// <summary>
    /// A status to indicate that the fulfillment was canceled by us and will not complete.
    /// </summary>
    Canceled = Terminated + Failure + ByUs,

    /// <summary>
    /// A status to indicate that the fulfillment was aborted by the game and will not complete.
    /// This can happen if we tried to send an invalid order.
    /// </summary>
    Aborted = Terminated + Failure + ByGameRules,

    /// <summary>
    /// A status to indicate that the fulfillment was prevented by the opponent and will not complete.
    /// </summary>
    Prevented = Terminated + Failure + ByOpponent
}
