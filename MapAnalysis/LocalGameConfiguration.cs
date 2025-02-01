using SC2APIProtocol;
using SC2Client;

namespace MapAnalysis;

public class LocalGameConfiguration : ILocalGameConfiguration {
    public string MapFileName { get; }
    public Race OpponentRace => Race.Terran;
    public Difficulty OpponentDifficulty => Difficulty.VeryEasy;
    public bool RealTime => false;

    public LocalGameConfiguration(string mapFileName) {
        MapFileName = mapFileName;
    }
}
