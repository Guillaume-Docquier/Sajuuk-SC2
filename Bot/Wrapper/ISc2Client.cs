using System.Threading.Tasks;
using SC2APIProtocol;

namespace Bot.Wrapper;

public interface ISc2Client {
    public Task Connect(string serverAddress, int gamePort, int maxRetries = 60);
    public Task<Response> SendRequest(Request request);
    public Task Quit();
}
