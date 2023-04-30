using SC2APIProtocol;

namespace Bot;

public interface INeedUpdating {
    // TODO GD Once DI is done, we shouldn't need to reset
    void Reset();

    void Update(ResponseObservation observation, ResponseGameInfo gameInfo);
}
