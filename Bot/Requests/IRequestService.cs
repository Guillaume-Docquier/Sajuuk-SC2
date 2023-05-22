using System.Threading.Tasks;
using SC2APIProtocol;

namespace Bot.Requests;

public interface IRequestService {
    public Task<Response> SendRequest(Request request, bool logErrors = false);
}
