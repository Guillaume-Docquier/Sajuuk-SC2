namespace Sajuuk.Builds;

/// <summary>
/// Represents the priority of a BuildRequest.
/// Higher priority means more important.
/// </summary>
public enum BuildRequestPriority {
    Low = -1,
    Normal = 0,
    BuildOrder = 1,
    Medium = 2,
    High = 3,
    VeryHigh = 4,
}
