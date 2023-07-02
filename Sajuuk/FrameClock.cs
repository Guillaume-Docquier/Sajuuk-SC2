using SC2APIProtocol;

namespace Sajuuk;

public class FrameClock : IFrameClock, INeedUpdating {
    public uint CurrentFrame { get; private set; } = uint.MaxValue;

    public void Reset() {
        CurrentFrame = uint.MaxValue;
    }

    public void Update(ResponseObservation observation, ResponseGameInfo gameInfo) {
        CurrentFrame = observation.Observation.GameLoop;
    }
}
