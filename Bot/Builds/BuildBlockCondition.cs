using System;

namespace Bot.Builds;

[Flags]
public enum BuildBlockCondition {
    None = 0,
    MissingTech = 1,
    MissingResources = 2,
    MissingProducer = 4,
    All = 8 - 1,
}
