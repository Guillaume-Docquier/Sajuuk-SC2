using SC2APIProtocol;

namespace SC2Client;

public interface ISc2Client {
    /**
     * Launches a StartCraft II instance.
     */
    public void LaunchSc2(string serverAddress, int gamePort);

    /**
     * Connects to a StarCraft II instance.
     */
    public Task ConnectToGameClient(string serverAddress, int gamePort, int maxRetries = 60);

    /**
     * Creates a StarCraft II game.
     */
    public Task CreateGame(string mapFileName, Race opponentRace, Difficulty opponentDifficulty, bool realTime);

    /**
     * Join a StarCraft II game.
     */
    public Task<uint> JoinGame(Request joinGameRequest);

    /**
     * Leave the Starcraft II game.
     */
    public Task LeaveCurrentGame();

    public Task<Response> SendRequest(Request request);
}
