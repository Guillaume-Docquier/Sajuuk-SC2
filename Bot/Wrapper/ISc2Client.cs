using System.Threading.Tasks;
using SC2APIProtocol;

namespace Bot.Wrapper;

public interface ISc2Client {
    public Task LaunchSc2(string serverAddress, int gamePort);
    public Task Connect(string serverAddress, int gamePort, int maxRetries = 60);
    public Task<Response> SendRequest(Request request, bool logErrors = false);
    public Task CreateGame(string mapFileName, Race opponentRace, Difficulty opponentDifficulty, bool realTime);
    public Task<uint> JoinGame(Request joinGameRequest);
    public Task LeaveCurrentGame();
}
