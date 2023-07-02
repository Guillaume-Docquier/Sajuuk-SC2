using SC2APIProtocol;

namespace Sajuuk;

// TODO GD We need a better name for this, trackers seems like the best option
public interface INeedUpdating {
    void Update(ResponseObservation observation, ResponseGameInfo gameInfo);
}
