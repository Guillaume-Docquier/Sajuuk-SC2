using SC2APIProtocol;

namespace Bot;

public interface INeedAccumulating {
    void Accumulate(ResponseObservation observation);
}
