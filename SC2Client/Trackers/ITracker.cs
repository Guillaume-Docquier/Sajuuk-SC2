using SC2Client.State;

namespace SC2Client.Trackers;

/// <summary>
/// A tracker tracks certain aspects of the parsed game state.
/// It is basically a customized view of some aspect of the state.
/// </summary>
public interface ITracker {
    /// <summary>
    /// Updates the tracker from the latest game state.
    /// </summary>
    /// <param name="gameState"></param>
    public void Update(IGameState gameState);
}
