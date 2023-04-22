using SC2APIProtocol;

namespace Bot;

public interface INeedUpdating {
    void Reset();

    void Update(ResponseObservation observation, ResponseGameInfo gameInfo);
}
