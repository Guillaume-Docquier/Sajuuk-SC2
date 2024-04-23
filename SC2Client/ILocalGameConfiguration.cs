using SC2APIProtocol;

namespace SC2Client;

public interface ILocalGameConfiguration {
    string MapFileName { get; }
    Race OpponentRace { get; }
    Difficulty OpponentDifficulty { get; }
    bool RealTime { get; }
}
