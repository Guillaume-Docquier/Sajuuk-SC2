using System.Numerics;
using System.Text.Json.Serialization;
using SC2Client.State;

namespace MapAnalysis.ExpandAnalysis;

public class ExpandLocation : IExpandLocation {
    [JsonInclude] public Vector2 OptimalTownHallPosition { get; private set;}
    [JsonInclude] public ExpandType ExpandType { get; private set;}
    [JsonIgnore] public HashSet<IUnit> Resources { get; }
    [JsonIgnore] public bool IsDepleted => !Resources.Any();
    [JsonIgnore] public bool IsObstructed => false;

    [JsonConstructor] public ExpandLocation() {}

    public ExpandLocation(Vector2 optimalTownHallPosition, ExpandType expandType, HashSet<IUnit> resources) {
        OptimalTownHallPosition = optimalTownHallPosition;
        ExpandType = expandType;
        Resources = resources;
    }
}
