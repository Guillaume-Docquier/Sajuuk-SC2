using System.Threading.Tasks;
using SC2APIProtocol;

namespace Bot.Wrapper;

public interface ISc2Client {
    public Task Connect(string address, int port);
    public Task<Response> SendRequest(Request request);
    public Task Ping();
    public Task Quit();
}
