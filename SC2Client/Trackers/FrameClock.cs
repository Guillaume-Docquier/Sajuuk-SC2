using SC2Client.State;

namespace SC2Client.Trackers;

public class FrameClock : IFrameClock, ITracker {
    public uint CurrentFrame { get; private set; } = 0;

    public void Update(IGameState gameState) {
        CurrentFrame = gameState.CurrentFrame;
    }
}
