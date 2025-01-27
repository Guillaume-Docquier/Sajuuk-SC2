using System.Numerics;
using SC2APIProtocol;

namespace SC2Client.State;

/// <summary>
/// The state for a single frame in a game.
/// The game state is a parsed version of the protobuf state of the game, with very little interpretation of it, if any.
/// The trackers are where you can transform the data to draw your own conclusions.
/// </summary>
public interface IGameState {
    /// <summary>
    /// The id of the player.
    /// </summary>
    public uint PlayerId { get; }

    /// <summary>
    /// The name of the map on which the game is being played.
    /// </summary>
    public string MapName { get; }

    /// <summary>
    /// The current game frame number.
    /// </summary>
    public uint CurrentFrame { get; }

    /// <summary>
    /// Indicates the result of the game.
    /// </summary>
    public Result Result { get; }

    /// <summary>
    /// The location of the starting townhall of the player.
    /// </summary>
    public Vector2 StartingLocation { get; }

    /// <summary>
    /// The location of the starting townhall of the enemy.
    /// </summary>
    public Vector2 EnemyStartingLocation { get; }

    /// <summary>
    /// The terrain data.
    /// </summary>
    public ITerrain Terrain { get; }

    /// <summary>
    /// The units data.
    /// </summary>
    public IUnits Units { get; }
}
