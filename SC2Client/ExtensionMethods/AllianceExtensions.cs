using SC2APIProtocol;

namespace SC2Client.ExtensionMethods;

public static class AllianceExtensions {
    public static Alliance GetOpposing(this Alliance alliance) {
        return alliance switch
        {
            Alliance.Self => Alliance.Enemy,
            Alliance.Enemy => Alliance.Self,
            _ => throw new ArgumentException($"The only possible alliances are Self and Enemy. But ${alliance} was provided."),
        };
    }
}
