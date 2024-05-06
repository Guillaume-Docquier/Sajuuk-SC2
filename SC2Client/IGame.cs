using SC2Client.State;

namespace SC2Client;

/// <summary>
/// This interface exposes all state and controls a human player would have while playing SC2.
/// This is what bots use to interact with the game while it is in progress.
/// </summary>
public interface IGame {
    /// <summary>
    /// The current state of the game
    /// </summary>
    public IGameState State { get; }

    /// <summary>
    /// Whether the game is still in progress
    /// </summary>
    public bool IsOver { get; }

    /// <summary>
    /// Submits all actions and advances the game simulation by a set number of frames.
    /// It is recommended to use a stepSize of at least 2 because new unit orders are never visible after only a single frame.
    /// The game State will be updated.
    /// </summary>
    /// <param name="stepSize">The number of game loops to simulate for the next frame. Must be greater than 0, or you'll automatically lose the game.</param>
    /// <param name="actions">The actions to perform.</param>
    public Task Step(uint stepSize, List<SC2APIProtocol.Action> actions);

    /// <summary>
    /// Quits the game immediately, abandoning it.
    /// </summary>
    public void Quit();
}
