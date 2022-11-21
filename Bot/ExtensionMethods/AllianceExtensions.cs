using SC2APIProtocol;

namespace Bot.ExtensionMethods;

public static class AllianceExtensions {
    public static Alliance GetOpposing(this Alliance alliance) {
        return alliance switch
        {
            Alliance.Ally => Alliance.Enemy,
            Alliance.Enemy => Alliance.Ally,
            _ => alliance
        };
    }
}
