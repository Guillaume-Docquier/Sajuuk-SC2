using System;

namespace Sajuuk.Builds.BuildRequests;

[Flags]
public enum BuildBlockCondition {
    None = 0,
    MissingTech = 1,
    MissingMinerals = 2,
    MissingVespene = 4,
    MissingResources = MissingMinerals + MissingVespene,
    MissingProducer = 8,
    All = 16 - 1,
}
