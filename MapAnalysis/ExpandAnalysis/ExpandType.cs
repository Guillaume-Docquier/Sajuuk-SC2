namespace MapAnalysis.ExpandAnalysis;

/// <summary>
/// A set of inferred expand types based on common terminology.
/// </summary>
public enum ExpandType {
    /// <summary>
    /// The main expand, where the player starts.
    /// </summary>
    Main,

    /// <summary>
    /// The first expand a player can take, down the main expand's ramp.
    /// </summary>
    Natural,

    /// <summary>
    /// A safe base, generally near the main and natural, that has less resources than a normal expansion.
    /// </summary>
    Pocket,

    Third,
    Fourth,
    Fifth,

    /// <summary>
    /// Anything beyond the fifth base.
    /// </summary>
    Far,

    /// <summary>
    /// A base with golden minerals or purple gas.
    /// </summary>
    Gold,
}
