using SC2APIProtocol;

namespace SC2Client;

/// <summary>
/// A client to handle StarCraft II instances.
/// This client is very close to the metal and offers very little abstractions over the game itself.
/// To get abstractions, use IGame instead.
/// </summary>
public interface ISc2Client {
    /// <summary>
    /// Launches a StartCraft II instance.
    /// </summary>
    /// <param name="serverAddress">The address of the server where the instance should run.</param>
    /// <param name="gamePort">The port that the server should use.</param>
    public void LaunchSc2(string serverAddress, int gamePort);

    /// <summary>
    /// Connects to a StarCraft II instance.
    /// </summary>
    /// <param name="serverAddress">The address of the server to connect to.</param>
    /// <param name="gamePort">The port of the server to connect to.</param>
    /// <param name="maxRetries">The number of retries to do before giving up.</param>
    public Task ConnectToGameClient(string serverAddress, int gamePort, int maxRetries = 60);

    /// <summary>
    /// Creates a StarCraft II local game against a computer opponent.
    /// </summary>
    /// <param name="localGameConfiguration">The local game configuration.</param>
    public Task CreateLocalGame(ILocalGameConfiguration localGameConfiguration);

    /// <summary>
    /// Joins a StarCraft II game.
    /// </summary>
    /// <param name="joinGameRequest">The request to join a game. This is typically a request to join a computer game, or an AIArena ladder game.</param>
    /// <returns>The assigned player id.</returns>
    public Task<uint> JoinGame(Request joinGameRequest);

    /// <summary>
    /// Leaves the current game.
    /// </summary>
    public Task LeaveCurrentGame();

    /// <summary>
    /// Sends an SC2 API request to the current game.
    /// This method will throw if you're not already connected to a game.
    /// </summary>
    /// <param name="request">The request to send.</param>
    /// <returns>The response of the request.</returns>
    public Task<Response> SendRequest(Request request);
}
