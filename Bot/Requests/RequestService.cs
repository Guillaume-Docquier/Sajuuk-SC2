using System.Threading.Tasks;
using Bot.Wrapper;
using SC2APIProtocol;

namespace Bot.Requests;

public class RequestService : IRequestService {
    private readonly IProtobufProxy _protobufProxy;

    public RequestService(IProtobufProxy protobufProxy) {
        _protobufProxy = protobufProxy;
    }

    public async Task<Response> SendRequest(Request request, bool logErrors = false) {
        var response = await _protobufProxy.SendRequest(request);
        if (logErrors) {
            LogResponseErrors(response);
        }

        return response;
    }

    private static void LogResponseErrors(Response response) {
        if (response.Error.Count > 0) {
            Logger.Error("Response errors:");
            foreach (var error in response.Error) {
                Logger.Error(error);
            }
        }
    }
}
