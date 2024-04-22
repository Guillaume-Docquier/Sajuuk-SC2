namespace SC2Client;

/// <summary>
/// A frame clock keeps track of the current frame.
/// </summary>
public interface IFrameClock {
    /// <summary>
    /// The current frame number.
    /// </summary>
    public uint CurrentFrame { get; }
}
