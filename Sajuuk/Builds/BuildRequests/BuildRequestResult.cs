using System;

namespace Sajuuk.Builds.BuildRequests;

[Flags]
public enum BuildRequestResult {
    Ok = 0,
    TechRequirementsNotMet = 1,
    NotEnoughMinerals = 2,
    NotEnoughVespeneGas = 4,
    NoProducersAvailable = 8,
    NotEnoughSupply = 16,
    NoSuitableLocation = 32,
    NotSupported = 64,
}
