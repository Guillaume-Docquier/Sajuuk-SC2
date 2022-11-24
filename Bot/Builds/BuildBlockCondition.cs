using System;

namespace Bot.Builds;

[Flags]
public enum BuildBlockCondition {
    None = 0,
    MissingTech = 1,
    MissingMinerals = 2,
    MissingVespene = 4,
    MissingResources = 2 + 4,
    MissingProducer = 8,
    All = 16 - 1,
}
