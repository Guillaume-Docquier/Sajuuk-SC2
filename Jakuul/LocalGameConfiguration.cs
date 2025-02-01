using SC2APIProtocol;
using SC2Client;

namespace Jakuul;

public class LocalGameConfiguration : ILocalGameConfiguration {
    public string MapFileName => Maps.CosmicSapphire;
    public Race OpponentRace => Race.Terran;
    public Difficulty OpponentDifficulty => Difficulty.CheatInsane;
    public bool RealTime => false;
}
