using Sajuuk.GameSense;
using SC2APIProtocol;

namespace Sajuuk.Builds.BuildRequests.Fulfillment;

public interface IBuildRequestFulfillmentFactory {
    IBuildRequestFulfillment CreateTrainUnitFulfillment(Unit producer, UnitOrder producerOrder, uint unitTypeToTrain);
}
