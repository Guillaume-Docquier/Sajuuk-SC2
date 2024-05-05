using SC2APIProtocol;

namespace SC2Client.State;

/// <summary>
/// The state for a single frame in a game.
/// </summary>
public interface IGameState {
    /// <summary>
    /// The id of the player.
    /// </summary>
    public uint PlayerId { get; }

    /// <summary>
    /// The current game frame number.
    /// </summary>
    public uint CurrentFrame { get; }

    /// <summary>
    /// Indicates the result of the game.
    /// </summary>
    public Result Result { get; }

    /// <summary>
    /// The terrain data.
    /// </summary>
    public ITerrain Terrain { get; }

    /// <summary>
    /// The units data.
    /// </summary>
    public IUnits Units { get; }
}
