using System.Numerics;
using System.Text.Json.Serialization;

namespace MapAnalysis.ExpandAnalysis;

public class ExpandLocation : IExpandLocation {
    [JsonInclude] public Vector2 OptimalTownHallPosition { get; private set;}
    [JsonInclude] public ExpandType ExpandType { get; private set;}

    [Obsolete("Do not use this parameterless JsonConstructor", error: true)]
    [JsonConstructor] public ExpandLocation() {}

    public ExpandLocation(Vector2 optimalTownHallPosition, ExpandType expandType) {
        OptimalTownHallPosition = optimalTownHallPosition;
        ExpandType = expandType;
    }
}
