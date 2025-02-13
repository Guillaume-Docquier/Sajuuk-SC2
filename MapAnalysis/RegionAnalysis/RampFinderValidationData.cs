using MapAnalysis.RegionAnalysis.Ramps;
using SC2Client.State;

namespace MapAnalysis.RegionAnalysis;

public class RampFinderValidationData {
    public const string FilenameTopic = "RampFinderTestsValidationData";

    public GameState InitialGameState { get; init; }
    public List<Ramp> ExpectedRamps { get; init; }
}
