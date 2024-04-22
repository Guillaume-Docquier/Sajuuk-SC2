namespace SC2Client;

/**
 * A frame clock keeps track of the current frame.
 */
public interface IFrameClock {
    /**
     * The current frame number.
     */
    public uint CurrentFrame { get; }
}
