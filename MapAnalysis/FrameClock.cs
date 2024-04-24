using SC2Client;

namespace MapAnalysis;

public class FrameClock : IFrameClock {
    public uint CurrentFrame { get; set; } = 0;
}
