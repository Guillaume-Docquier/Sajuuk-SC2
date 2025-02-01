using System.Numerics;
using System.Text.Json.Serialization;
using SC2Client.State;

namespace MapAnalysis.ExpandAnalysis;

public class ExpandLocation : IExpandLocation {
    [JsonInclude] public Vector2 OptimalTownHallPosition { get; private set;}
    [JsonInclude] public ExpandType ExpandType { get; private set;}

    /// <summary>
    /// We don't serialize the resources because
    /// </summary>
    [JsonIgnore] public IReadOnlyList<IUnit> Resources { get; }

    [Obsolete("Do not use this parameterless JsonConstructor", error: true)]
    [JsonConstructor] public ExpandLocation() {}

    public ExpandLocation(Vector2 optimalTownHallPosition, ExpandType expandType, List<IUnit> resources) {
        OptimalTownHallPosition = optimalTownHallPosition;
        ExpandType = expandType;
        Resources = resources;
    }
}
