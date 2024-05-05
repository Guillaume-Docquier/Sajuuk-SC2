using SC2Client.State;

namespace SC2Client.Trackers;

/// <summary>
/// A tracker tracks certain aspects of the game state.
/// </summary>
public interface ITracker {
    /// <summary>
    /// Updates the tracker from the latest game state.
    /// </summary>
    /// <param name="gameState"></param>
    public void Update(IGameState gameState);
}
