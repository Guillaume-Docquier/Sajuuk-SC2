using SC2APIProtocol;
using SC2Client.GameData;
using SC2Client.GameState;

namespace SC2Client;

/// <summary>
/// This interface exposes all state and controls a human player would have while playing SC2.
/// This is what bots use to interact with the game while it is in progress.
/// </summary>
public interface IGame {
    /// <summary>
    /// The current game frame number.
    /// </summary>
    public uint CurrentFrame { get; } // TODO GD Wrap all state into an IGameState?

    /// <summary>
    /// Indicates the result of the game.
    /// </summary>
    public Result GameResult { get; }

    /// <summary>
    /// Exposes game data about units, structures, abilities, etc.
    /// </summary>
    public KnowledgeBase KnowledgeBase { get; }

    /// <summary>
    /// The terrain data.
    /// </summary>
    public ITerrain Terrain { get; }

    /// <summary>
    /// Submits all actions and advances the game simulation by a set number of frames.
    /// The game state will be updated.
    /// </summary>
    /// <param name="stepSize">The number of game loops to simulate for the next frame.</param>
    public Task Step(uint stepSize);

    /// <summary>
    /// Abandons the game immediately, leaving it.
    /// </summary>
    public void Surrender();
}
